using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Warns when a SqlArtisan construct is used against a configured target
/// dialect set it is not supported on (#93 / ADR 0003, set-valued per #432).
/// Silent until <c>sqlartisan_syntax_*</c> (or the legacy
/// <c>sqlartisan_target_dbms</c>) is set; only ever warns about constructs the
/// matrix has a verified entry for (never a false positive from an incomplete
/// matrix).
/// </summary>
/// <remarks>
/// Coupling to the core library is limited to a three-point contract
/// (ADR 0009): the containing-assembly name (<c>"SqlArtisan"</c>), the public
/// member names the matrix keys mirror (gate-enforced both ways by the
/// integrity and coverage tests), and the <c>.editorconfig</c> / MSBuild
/// configuration surface. Do not add a build reference to SqlArtisan or share
/// types with it — the analyzer must stay loadable and correct against any
/// core version.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DialectUsageAnalyzer : DiagnosticAnalyzer
{
    private const string SqlArtisanAssemblyName = "SqlArtisan";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.InvalidConfiguration,
        DiagnosticDescriptors.UnrecognizedConfigurationKey,
        DiagnosticDescriptors.ConfigurationDisablesAllDialects,
        DiagnosticDescriptors.LegacyConfigurationIgnored,
        DiagnosticDescriptors.LegacyConfigDeprecated,
        DiagnosticDescriptors.UnsupportedDialectConstruct,
        DiagnosticDescriptors.VersionBoundConstruct,
        DiagnosticDescriptors.ContextRestrictedConstruct,
        DiagnosticDescriptors.IdentifierTooLong,
        DiagnosticDescriptors.ConstantNullPredicate,
        DiagnosticDescriptors.NotInNullableSubquery,
        DiagnosticDescriptors.InsertMissingRequiredColumn,
        DiagnosticDescriptors.CountNullableColumn,
        DiagnosticDescriptors.UnusableIndexPredicate,
        DiagnosticDescriptors.TypeCategoryMismatch,
        DiagnosticDescriptors.CorrelatedDmlTargetNotAliased);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    // One target-set cache per compilation: resolving sqlartisan_syntax_* used
    // to cost up to 10 AnalyzerConfigOptions lookups plus an EngineVersion
    // parse per DBMS on every single usage. Caching by SyntaxTree collapses
    // that to one dictionary lookup per usage (concurrent — EnableConcurrentExecution
    // above lets operation actions for different trees run in parallel).
    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var targetCache = new ConcurrentDictionary<SyntaxTree, DialectTargetSet>();

        // The generic walkers first — they serve SQLA0100/0101 together and key on
        // the operation kind, not a rule — then one dispatcher per rule in ID order,
        // then the compilation-end action, which is not an operation action at all.
        context.RegisterOperationAction(c => AnalyzeInvocation(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzePropertyReference(c, targetCache), OperationKind.PropertyReference);
        context.RegisterOperationAction(c => AnalyzeFieldReference(c, targetCache), OperationKind.FieldReference);
        context.RegisterOperationAction(c => AnalyzeBinaryOperator(c, targetCache), OperationKind.Binary);
        context.RegisterOperationAction(c => AnalyzeCompoundAssignment(c, targetCache), OperationKind.CompoundAssignment);
        context.RegisterOperationAction(c => AnalyzeContextRules(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeIdentifierLength(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeIdentifierLength(c, targetCache), OperationKind.ObjectCreation);
        context.RegisterOperationAction(c => AnalyzeSchemaNullability(c, targetCache), OperationKind.PropertyReference);
        context.RegisterOperationAction(c => AnalyzeNotInSubquery(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeInsertColumns(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeCountArgument(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeIndexedColumnFilter(c, targetCache), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeTypeCategoryMismatch(c, targetCache), OperationKind.Binary);
        context.RegisterOperationAction(c => AnalyzeCorrelatedDml(c, targetCache), OperationKind.Invocation);
        context.RegisterCompilationEndAction(ValidateConfiguration);
    }

    private static DialectTargetSet GetTargets(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache) =>
        cache.GetOrAdd(context.Operation.Syntax.SyntaxTree, tree =>
            AnalyzerConfigResolver.ResolveTargets(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree)));

    private static void AnalyzeInvocation(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        IMethodSymbol method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (!IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        IPropertySymbol property = ((IPropertyReferenceOperation)context.Operation).Property;
        if (!IsFromSqlArtisan(property.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, property.Name, arity: null);
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        IFieldSymbol field = ((IFieldReferenceOperation)context.Operation).Field;
        if (!IsFromSqlArtisan(field.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, field.Name, arity: null);
    }

    // Overloaded C# operators (#219) reach Roslyn as Binary / CompoundAssignment operations,
    // never as invocations; OperatorMethod is null for built-in operators.
    private static void AnalyzeBinaryOperator(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        if (((IBinaryOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeTypeCategoryMismatch(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        TypeCategoryMismatchRule.Check(context, (IBinaryOperation)context.Operation);
    }

    private static void AnalyzeCompoundAssignment(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        if (((ICompoundAssignmentOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeUsage(
        OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache, string memberName, int? arity)
    {
        DialectTargetSet targets = GetTargets(context, cache);
        if (targets.IsEmpty)
        {
            return;
        }

        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);

        // The override is the user's own claim about their configuration, dialect-independent,
        // so it is resolved once per usage — never re-evaluated per DBMS (#432).
        if (DialectSupportResolver.ResolveOverride(options, memberName, arity) is { } overrideResult)
        {
            if (overrideResult.IsSupported)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnsupportedDialectConstruct,
                context.Operation.Syntax.GetLocation(),
                DisplayName(memberName, arity, overrideResult.IsArityLevel),
                JoinDisplayNames([.. targets.Members.Select(TargetDbmsNames.Display)]),
                overrideResult.OverrideKeyHint));
            return;
        }

        if (DialectSupportResolver.MatchMatrixEntry(memberName, arity) is not { } match)
        {
            return;
        }

        List<string>? unsupportedOn = null;
        List<(TargetDbms Dbms, string RequiredVersion, EngineVersion? DeclaredVersion)>? versionBound = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, dbms, targets.VersionFor(dbms));
            if (verdict.IsSupported)
            {
                continue;
            }

            if (verdict.IsVersionBound)
            {
                (versionBound ??= []).Add((dbms, verdict.RequiredVersion!, targets.VersionFor(dbms)));
            }
            else
            {
                (unsupportedOn ??= []).Add(TargetDbmsNames.Display(dbms));
            }
        }

        if (unsupportedOn is { Count: > 0 })
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnsupportedDialectConstruct,
                context.Operation.Syntax.GetLocation(),
                DisplayName(memberName, arity, match.IsArityLevel),
                JoinDisplayNames(unsupportedOn),
                match.OverrideKeyHint));
        }

        if (versionBound is not null)
        {
            foreach ((TargetDbms dbms, string requiredVersion, EngineVersion? declaredVersion) in versionBound)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.VersionBoundConstruct,
                    context.Operation.Syntax.GetLocation(),
                    DisplayName(memberName, arity, match.IsArityLevel),
                    TargetDbmsNames.Display(dbms),
                    requiredVersion,
                    declaredVersion,
                    match.OverrideKeyHint));
            }
        }
    }

    // Name-filter first so config resolution is paid only on trigger names. Each
    // rule pairs its trigger with the one dialect whose grammar restricts it;
    // elsewhere the matrix entry already answers (#264).
    private static void AnalyzeContextRules(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        string name = invocation.TargetMethod.Name;
        if (name is not ("Limit" or "Grouping" or "PercentileCont" or "PercentileDisc"
                or "Inserted" or "Deleted")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        DialectTargetSet targets = GetTargets(context, cache);
        switch (name)
        {
            case "Limit" when targets.Contains(TargetDbms.MySql):
                ContextRules.CheckLimitInQuantifiedSubquery(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.MySql));
                break;
            case "Grouping" when targets.Contains(TargetDbms.MySql):
                ContextRules.CheckGroupingRequiresWithRollup(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.MySql));
                break;
            case "PercentileCont" or "PercentileDisc" when targets.Contains(TargetDbms.SqlServer):
                ContextRules.CheckPercentileRequiresOver(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.SqlServer));
                break;
            case "Inserted" or "Deleted" when targets.Contains(TargetDbms.SqlServer):
                ContextRules.CheckPseudoTableRequiresOutput(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.SqlServer));
                break;
        }
    }

    // IsNull / IsNotNull are SqlExpression properties, so the column under test is
    // the receiver. Gated on a configured target set like every other rule, though
    // the verdict itself is dialect-independent.
    private static void AnalyzeSchemaNullability(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (reference.Property.Name is not ("IsNull" or "IsNotNull")
            || !IsFromSqlArtisan(reference.Property.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        ConstantNullPredicateRule.Check(context, reference);
    }

    // The value overloads take the same name and arity, so the parameter type is
    // what selects the subquery form.
    private static void AnalyzeNotInSubquery(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "NotIn"
            || invocation.Arguments.Length != 1
            || invocation.TargetMethod.Parameters[0].Type.ToDisplayString() != "SqlArtisan.ISubquery"
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        NotInNullableSubqueryRule.Check(context, invocation);
    }

    // Only the explicit-column-list overload: the positional form supplies every
    // column by construction, and InsertIgnoreInto asked for failures to be
    // skipped, which is what omitting a required column would produce.
    private static void AnalyzeInsertColumns(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "InsertInto"
            || invocation.Arguments.Length != 2
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        InsertMissingRequiredColumnRule.Check(context, invocation);
    }

    // Count(Asterisk) shares the arity, and COUNT(DISTINCT col) is asking for
    // values by construction, so only the plain object overload is a candidate.
    private static void AnalyzeCountArgument(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "Count"
            || invocation.Arguments.Length != 1
            || invocation.TargetMethod.Parameters[0].Type.SpecialType != SpecialType.System_Object
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        CountNullableColumnRule.Check(context, invocation);
    }

    // Like carries the column as its receiver; every other shape wraps it as an
    // argument, so the two enter the rule by different doors.
    private static void AnalyzeIndexedColumnFilter(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        if (invocation.TargetMethod.Name is "Like" or "NotLike" && invocation.Arguments.Length == 1)
        {
            UnusableIndexPredicateRule.CheckLike(context, invocation);
            return;
        }

        UnusableIndexPredicateRule.CheckFunctionCall(context, invocation);
    }

    // Both DML heads (#256) — the static Sql members and the WithBuilder instance
    // methods — share the name and the DbTableBase-first-parameter shape.
    private static void AnalyzeCorrelatedDml(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("Update" or "DeleteFrom")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        CorrelatedDmlRule.Check(context, invocation);
    }

    private static void AnalyzeIdentifierLength(OperationAnalysisContext context, ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache)
    {
        (IMethodSymbol? member, ImmutableArray<IArgumentOperation> arguments) = context.Operation switch
        {
            IInvocationOperation invocation => (invocation.TargetMethod, invocation.Arguments),
            IObjectCreationOperation { Constructor: { } constructor } creation => (constructor, creation.Arguments),
            _ => (null, default),
        };

        if (member is null || !IsFromSqlArtisan(member.ContainingAssembly))
        {
            return;
        }

        DialectTargetSet targets = GetTargets(context, cache);
        if (targets.IsEmpty)
        {
            return;
        }

        IdentifierLengthRule.Check(context, member, arguments, targets);
    }

    private static void ValidateConfiguration(CompilationAnalysisContext context)
    {
        var reportedTargetValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reportedVersionValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reportedOverrideValues = new HashSet<(string Key, string Value)>();
        string[] overrideKeys = [.. DialectMatrix.AllOverrideKeys.Distinct()];
        string validTargetNames = string.Join("/", AnalyzerConfigResolver.ValidTargetNames);

        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            if (options.TryGetValue(AnalyzerConfigResolver.TargetDbmsKey, out string? targetValue)
                && !AnalyzerConfigResolver.IsRecognizedTargetValue(targetValue)
                && reportedTargetValues.Add(targetValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidConfiguration,
                    Location.None,
                    AnalyzerConfigResolver.TargetDbmsKey,
                    targetValue,
                    $"one of: {validTargetNames}"));
            }

            if (options.TryGetValue(AnalyzerConfigResolver.TargetVersionKey, out string? versionValue)
                && !AnalyzerConfigResolver.IsRecognizedVersionValue(versionValue)
                && reportedVersionValues.Add(versionValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidConfiguration,
                    Location.None,
                    AnalyzerConfigResolver.TargetVersionKey,
                    versionValue,
                    "a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));
            }

            foreach (string overrideKey in overrideKeys)
            {
                if (options.TryGetValue(overrideKey, out string? overrideValue)
                    && !AnalyzerConfigResolver.IsRecognizedOverrideValue(overrideValue)
                    && reportedOverrideValues.Add((overrideKey, overrideValue)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidConfiguration,
                        Location.None,
                        overrideKey,
                        overrideValue,
                        "supported/unsupported"));
                }
            }
        }

        ValidateSyntaxFamily(context);
    }

    // Four more SQLA0001 reasons plus the separate SQLA0002 nag (#432), each
    // deduplicated across trees at the granularity its message varies by: a
    // key name, a (key, value) pair, the dropped DBMS, or — for the two
    // compilation-wide facts (empty set, legacy-alone) — a single flag.
    private static void ValidateSyntaxFamily(CompilationAnalysisContext context)
    {
        var reportedUnrecognizedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reportedSyntaxValues = new HashSet<(string Key, string Value)>();
        var reportedDroppedDbms = new HashSet<TargetDbms>();
        bool reportedEmptySet = false;
        bool reportedDeprecation = false;
        string validDbmsNames = string.Join("/", AnalyzerConfigResolver.ValidTargetNames);

        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
            bool familyPresent = AnalyzerConfigResolver.IsFamilyPresent(options);

            if (AnalyzerConfigResolver.TryEnumerateSyntaxKeys(options, out List<string> syntaxKeys))
            {
                foreach (string key in syntaxKeys)
                {
                    if (!AnalyzerConfigResolver.IsRecognizedSyntaxKey(key) && reportedUnrecognizedKeys.Add(key))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.UnrecognizedConfigurationKey,
                            Location.None,
                            key,
                            validDbmsNames));
                    }
                }
            }

            bool hasUnrecognizedSyntaxValue = false;
            foreach ((string key, string value) in AnalyzerConfigResolver.SetSyntaxValues(options))
            {
                if (AnalyzerConfigResolver.IsRecognizedSyntaxValue(value))
                {
                    continue;
                }

                hasUnrecognizedSyntaxValue = true;
                if (reportedSyntaxValues.Add((key, value)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidConfiguration,
                        Location.None,
                        key,
                        value,
                        "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));
                }
            }

            // An unrecognized value already explains why this tree's set came up
            // empty (SQLA0001 reason 2 above) — reporting the empty-set reason too
            // would duplicate the same root cause under two descriptors.
            if (familyPresent && !hasUnrecognizedSyntaxValue && !reportedEmptySet
                && AnalyzerConfigResolver.ResolveTargets(options).IsEmpty)
            {
                reportedEmptySet = true;
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ConfigurationDisablesAllDialects, Location.None));
            }

            // Only when the family does not itself name the legacy DBMS — the
            // report exists for the silent replacement, and a family key set for
            // that DBMS is the user's own statement about it (ADR 0019).
            if (familyPresent
                && AnalyzerConfigResolver.ResolveTarget(options) is { } droppedDbms
                && !AnalyzerConfigResolver.IsFamilyKeySet(options, droppedDbms)
                && reportedDroppedDbms.Add(droppedDbms))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LegacyConfigurationIgnored,
                    Location.None,
                    AnalyzerConfigResolver.TargetDbmsKey,
                    LegacyDbmsRawValue(options, droppedDbms),
                    TargetDbmsNames.Display(droppedDbms),
                    AnalyzerConfigResolver.SyntaxKey(droppedDbms)));
            }

            if (!familyPresent && !reportedDeprecation
                && (AnalyzerConfigResolver.ResolveTarget(options) is not null
                    || AnalyzerConfigResolver.ResolveTargetVersion(options) is not null))
            {
                reportedDeprecation = true;
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LegacyConfigDeprecated,
                    Location.None,
                    LegacyReplacementSuggestion(options)));
            }
        }
    }

    private static string LegacyDbmsRawValue(AnalyzerConfigOptions options, TargetDbms resolved)
    {
        if (options.TryGetValue(AnalyzerConfigResolver.TargetDbmsKey, out string? editorConfigValue)
            && AnalyzerConfigResolver.IsRecognizedTargetValue(editorConfigValue))
        {
            return editorConfigValue;
        }

        if (options.TryGetValue(AnalyzerConfigResolver.TargetDbmsMSBuildPropertyKey, out string? msBuildValue)
            && AnalyzerConfigResolver.IsRecognizedTargetValue(msBuildValue))
        {
            return msBuildValue;
        }

        return resolved.ToString();
    }

    private static string LegacyReplacementSuggestion(AnalyzerConfigOptions options) =>
        AnalyzerConfigResolver.ResolveTarget(options) is { } dbms
            ? $"{AnalyzerConfigResolver.SyntaxKey(dbms)} = {AnalyzerConfigResolver.ResolveTargetVersion(options)?.ToString() ?? AnalyzerConfigResolver.AnyValue}"
            : "sqlartisan_syntax_<dbms> = <version-or-any>";

    internal static bool IsFromSqlArtisan(IAssemblySymbol? assembly) => assembly?.Name == SqlArtisanAssemblyName;

    // "MySQL", "MySQL and Oracle", "MySQL, Oracle and PostgreSQL" — the SQLA0100
    // join wording for a construct that fails on more than one configured DBMS.
    private static string JoinDisplayNames(IReadOnlyList<string> names) => names.Count switch
    {
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1],
    };

    private static string DisplayName(string memberName, int? arity, bool isArityLevel) =>
        OperatorDisplayName(memberName)
        ?? (isArityLevel && arity.HasValue ? $"{memberName} ({arity.Value}-argument form)" : memberName);

    // Users write the C# glyph, not the CLR method name — show "operator %", not "op_Modulus".
    // The override key in the message still derives from the CLR name (sqlartisan_construct_op_modulus).
    private static string? OperatorDisplayName(string memberName) => memberName switch
    {
        "op_Addition" => "operator +",
        "op_Subtraction" => "operator -",
        "op_Multiply" => "operator *",
        "op_Division" => "operator /",
        "op_Modulus" => "operator %",
        "op_Equality" => "operator ==",
        "op_Inequality" => "operator !=",
        "op_LessThan" => "operator <",
        "op_GreaterThan" => "operator >",
        "op_LessThanOrEqual" => "operator <=",
        "op_GreaterThanOrEqual" => "operator >=",
        "op_BitwiseAnd" => "operator &",
        "op_BitwiseOr" => "operator |",
        _ => null,
    };
}
