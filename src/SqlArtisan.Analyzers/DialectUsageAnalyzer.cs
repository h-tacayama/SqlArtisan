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
/// Warns when a SqlArtisan construct is used against a configured target dialect
/// set it is not supported on (#93 / ADR 0003, set-valued per #432). Silent until
/// a target is configured, and only for constructs the matrix verifiably covers.
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
        DiagnosticDescriptors.InvalidConfiguration,
        DiagnosticDescriptors.UnrecognizedConfigurationKey,
        DiagnosticDescriptors.ConfigurationDisablesAllDialects,
        DiagnosticDescriptors.RemovedConfigurationKey,
        DiagnosticDescriptors.UnsupportedDialectConstruct,
        DiagnosticDescriptors.VersionBoundConstruct,
        DiagnosticDescriptors.ContextRestrictedConstruct,
        DiagnosticDescriptors.IdentifierTooLong,
        DiagnosticDescriptors.InvalidDatepartArgument,
        DiagnosticDescriptors.InvalidArgumentValue,
        DiagnosticDescriptors.ConstantNullPredicate,
        DiagnosticDescriptors.NotInNullableSubquery,
        DiagnosticDescriptors.InsertMissingRequiredColumn,
        DiagnosticDescriptors.CountNullableColumn,
        DiagnosticDescriptors.UnusableIndexPredicate,
        DiagnosticDescriptors.TypeCategoryMismatch,
        DiagnosticDescriptors.CorrelatedDmlTargetNotAliased,
        DiagnosticDescriptors.ReferenceEqualityBound,
        DiagnosticDescriptors.QueryObjectAsText);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    // One target-set cache per compilation: resolving sqlartisan_syntax_* cost up to
    // 10 config lookups plus a version parse per usage; caching by SyntaxTree collapses
    // that to one lookup (concurrent — operation actions run in parallel across trees).
    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var targetCache = new ConcurrentDictionary<SyntaxTree, DialectTargetSet>();

        // Generic walkers first (they serve SQLA0100/0101 and key on operation kind),
        // then one dispatcher per rule in ID order, then the compilation-end action.
        context.RegisterOperationAction(
            c => AnalyzeInvocation(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzePropertyReference(c, targetCache),
            OperationKind.PropertyReference);
        context.RegisterOperationAction(
            c => AnalyzeFieldReference(c, targetCache),
            OperationKind.FieldReference);
        context.RegisterOperationAction(
            c => AnalyzeBinaryOperator(c, targetCache),
            OperationKind.Binary);
        context.RegisterOperationAction(
            c => AnalyzeCompoundAssignment(c, targetCache),
            OperationKind.CompoundAssignment);
        context.RegisterOperationAction(
            c => AnalyzeContextRules(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeIdentifierLength(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeIdentifierLength(c, targetCache),
            OperationKind.ObjectCreation);
        context.RegisterOperationAction(
            c => AnalyzeDatepartValidity(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeArgumentValueValidity(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeSchemaNullability(c, targetCache),
            OperationKind.PropertyReference);
        context.RegisterOperationAction(
            c => AnalyzeNotInSubquery(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeInsertColumns(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeCountArgument(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeIndexedColumnFilter(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeTypeCategoryMismatch(c, targetCache),
            OperationKind.Binary);
        context.RegisterOperationAction(
            c => AnalyzeCorrelatedDml(c, targetCache),
            OperationKind.Invocation);
        context.RegisterOperationAction(
            c => AnalyzeCSharpFallback(c, targetCache),
            OperationKind.Binary);
        context.RegisterOperationAction(
            c => AnalyzeInterpolation(c, targetCache),
            OperationKind.InterpolatedString);
        context.RegisterCompilationEndAction(ValidateConfiguration);
    }

    private static DialectTargetSet GetTargets(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache) =>
        cache.GetOrAdd(context.Operation.Syntax.SyntaxTree, tree =>
            AnalyzerConfigResolver.ResolveTargets(
                context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree)));

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        IMethodSymbol method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (!IsConstructMember(method))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzePropertyReference(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        IPropertySymbol property = ((IPropertyReferenceOperation)context.Operation).Property;
        if (!IsConstructMember(property))
        {
            return;
        }

        AnalyzeUsage(context, cache, property.Name, arity: null);
    }

    private static void AnalyzeFieldReference(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        IFieldSymbol field = ((IFieldReferenceOperation)context.Operation).Field;
        if (!IsConstructMember(field))
        {
            return;
        }

        AnalyzeUsage(context, cache, field.Name, arity: null);
    }

    // Overloaded C# operators (#219) reach Roslyn as Binary / CompoundAssignment operations,
    // never as invocations; OperatorMethod is null for built-in operators.
    private static void AnalyzeBinaryOperator(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        if (((IBinaryOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeTypeCategoryMismatch(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        TypeCategoryMismatchRule.Check(context, (IBinaryOperation)context.Operation);
    }

    private static void AnalyzeCompoundAssignment(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        if (((ICompoundAssignmentOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, cache, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeUsage(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree, DialectTargetSet> cache,
        string memberName,
        int? arity)
    {
        DialectTargetSet targets = GetTargets(context, cache);
        if (targets.IsEmpty)
        {
            return;
        }

        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(
            context.Operation.Syntax.SyntaxTree);

        // The override is the user's own claim about their configuration, dialect-independent,
        // so it is resolved once per usage — never re-evaluated per DBMS (#432).
        if (DialectSupportResolver.ResolveOverride(options, memberName, arity)
            is { } overrideResult)
        {
            if (overrideResult.IsSupported)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnsupportedDialectConstruct,
                context.Operation.Syntax.GetLocation(),
                DisplayName(memberName, arity, overrideResult.IsArityLevel),
                TargetDbmsNames.JoinDisplayNames(
                    [.. targets.Members.Select(TargetDbmsNames.Display)]),
                overrideResult.OverrideKeyHint));
            return;
        }

        if (DialectSupportResolver.MatchMatrixEntry(memberName, arity) is not { } match)
        {
            return;
        }

        List<string>? unsupportedOn = null;
        List<(TargetDbms Dbms, string RequiredVersion, EngineVersion? DeclaredVersion)>?
            versionBound = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(
                match,
                dbms,
                targets.VersionFor(dbms));
            if (verdict.IsSupported)
            {
                continue;
            }

            if (verdict.IsVersionBound)
            {
                (versionBound ??= []).Add(
                    (dbms, verdict.RequiredVersion!, targets.VersionFor(dbms)));
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
                TargetDbmsNames.JoinDisplayNames(unsupportedOn),
                match.OverrideKeyHint));
        }

        if (versionBound is not null)
        {
            foreach ((TargetDbms dbms, string requiredVersion, EngineVersion? declaredVersion)
                in versionBound)
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

    // Name-filter first so config resolution is paid only on trigger names. A rule
    // names every dialect whose grammar restricts its trigger — one for the walking
    // rules, a set for the DML shapes; elsewhere the matrix entry answers (#264).
    private static void AnalyzeContextRules(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        string name = invocation.TargetMethod.Name;
        if (name is not ("Limit" or "Grouping" or "PercentileCont" or "PercentileDisc"
                or "Inserted" or "Deleted" or "Interval" or "IntervalLiteral"
                or "From" or "Using" or "InnerJoin" or "LeftJoin" or "RightJoin"
                or "ForUpdate" or "Over" or "With" or "Returning" or "Where" or "Values"
                or "WhenMatched" or "WhenNotMatched" or "WhenNotMatchedBySource")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        // These names are on every query's hot path, so this name compare runs
        // before the per-tree config lookup and drops all but the DML spellings.
        ContextRules.DmlShape shape = ContextRules.ClassifyDmlShape(invocation);
        if (shape == ContextRules.DmlShape.None
            && name is "From" or "Using" or "InnerJoin" or "LeftJoin" or "RightJoin" or "With"
                or "Where" or "Values")
        {
            return;
        }

        DialectTargetSet targets = GetTargets(context, cache);
        if (shape != ContextRules.DmlShape.None)
        {
            if (RejectingDmlTargets(targets, shape) is { } names)
            {
                ContextRules.ReportDmlShape(context, invocation, shape, names);
            }

            return;
        }

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
            case "Over" when targets.Contains(TargetDbms.SqlServer):
                ContextRules.CheckUnorderedWindowRequiresOrderBy(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.SqlServer));
                break;
            case "Inserted" or "Deleted" when targets.Contains(TargetDbms.SqlServer):
                ContextRules.CheckPseudoTableRequiresOutput(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.SqlServer));
                break;
            case "Interval" when targets.Contains(TargetDbms.MySql):
            // IntervalLiteral's other arities are already mySql:false in the matrix;
            // only arity-2 is the coincidental accept this rule exists for.
            case "IntervalLiteral" when targets.Contains(TargetDbms.MySql)
                && invocation.TargetMethod.Parameters.Length == 2:
                ContextRules.CheckIntervalRequiresArithmeticOperand(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.MySql));
                break;
            case "Returning" when targets.Contains(TargetDbms.Oracle):
                ContextRules.CheckReturningRequiresInto(
                    context, invocation, TargetDbmsNames.Display(TargetDbms.Oracle));
                break;
            case "WhenMatched" or "WhenNotMatched" or "WhenNotMatchedBySource":
                ContextRules.CheckRepeatedMergeBranch(context, invocation, targets);
                break;
            case "ForUpdate":
                if (ContextRules.RejectingTargets(targets, s_groupedLockUnsupported)
                    is { } groupedNames)
                {
                    ContextRules.CheckForUpdateAfterGroupBy(context, invocation, groupedNames);
                }

                if (ContextRules.RejectingTargets(targets, s_rowLimitedLockUnsupported)
                    is { } limitedNames)
                {
                    ContextRules.CheckForUpdateAfterRowLimiting(context, invocation, limitedNames);
                }

                if (targets.Contains(TargetDbms.Oracle))
                {
                    ContextRules.CheckForUpdateInSubquery(
                        context, invocation, TargetDbmsNames.Display(TargetDbms.Oracle));
                }

                break;
        }
    }

    // Enum declaration order, so a message listing several reads in the order
    // docs-style.md fixes. A dialect is absent when it parses the spelling, when
    // the matrix already calls the member unsupported, or when Build(Dbms) throws.
    private static readonly TargetDbms[] s_groupedLockUnsupported =
        [TargetDbms.Oracle, TargetDbms.PostgreSql];

    // Oracle alone: PostgreSQL runs the pairing, and on the rest the matrix already
    // calls either the row-limiting step or ForUpdate unsupported.
    private static readonly TargetDbms[] s_rowLimitedLockUnsupported = [TargetDbms.Oracle];

    private static readonly TargetDbms[] s_joinedDmlUnsupported =
        [TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite];

    private static readonly TargetDbms[] s_deleteUsingUnsupported = [TargetDbms.Oracle];

    private static readonly TargetDbms[] s_updateFromUnsupported =
        [TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.Sqlite];

    // Oracle 21c rejects the table value constructor 23ai added (#87).
    private static readonly TargetDbms[] s_valuesRowUnsupported = [TargetDbms.Oracle];

    private static readonly TargetDbms[] s_insertSelectWithUnsupported = [TargetDbms.SqlServer];

    // MySQL and SQLite are absent because the matrix already calls MergeInto unsupported there.
    private static readonly TargetDbms[] s_mergeActionWhereUnsupported =
        [TargetDbms.PostgreSql, TargetDbms.SqlServer];

    // Static instances, not a collection expression per call: this runs on every
    // joined-DML step of every compilation (ADR 0006).
    private static TargetDbms[] RejectingDialects(ContextRules.DmlShape shape) => shape switch
    {
        ContextRules.DmlShape.JoinedDeleteLead => s_joinedDmlUnsupported,
        ContextRules.DmlShape.DeleteUsing => s_deleteUsingUnsupported,
        ContextRules.DmlShape.JoinedUpdateJoin => s_joinedDmlUnsupported,
        ContextRules.DmlShape.InsertSelectWith => s_insertSelectWithUnsupported,
        ContextRules.DmlShape.MergeActionWhere => s_mergeActionWhereUnsupported,
        ContextRules.DmlShape.InsertValuesRow => s_valuesRowUnsupported,
        _ => s_updateFromUnsupported,
    };

    // The version a rejecting dialect starts accepting the shape at, read against the
    // declared version or, with none, the matrix baseline, as a Bounds row is (ADR 0015).
    // Internal for the provenance gate, which ties each floor to docs/analyzer.md.
    internal static readonly (ContextRules.DmlShape Shape, TargetDbms Dbms, EngineVersion From)[]
        DmlShapeFloors =
        [
            (ContextRules.DmlShape.JoinedUpdateFrom, TargetDbms.Oracle, DialectMatrix.V("23")),
            (ContextRules.DmlShape.JoinedUpdateFrom, TargetDbms.Sqlite, DialectMatrix.V("3.33")),
            (ContextRules.DmlShape.InsertValuesRow, TargetDbms.Oracle, DialectMatrix.V("23")),
            (ContextRules.DmlShape.DeleteUsing, TargetDbms.Oracle, DialectMatrix.V("23")),
        ];

    private static EngineVersion? AcceptedFrom(ContextRules.DmlShape shape, TargetDbms dbms)
    {
        foreach ((ContextRules.DmlShape Shape, TargetDbms Dbms, EngineVersion From) floor
            in DmlShapeFloors)
        {
            if (floor.Shape == shape && floor.Dbms == dbms)
            {
                return floor.From;
            }
        }

        return null;
    }

    private static string? RejectingDmlTargets(
        DialectTargetSet targets,
        ContextRules.DmlShape shape)
    {
        List<string>? names = null;
        foreach (TargetDbms dbms in RejectingDialects(shape))
        {
            if (!targets.Contains(dbms))
            {
                continue;
            }

            if (AcceptedFrom(shape, dbms) is { } floor
                && (targets.VersionFor(dbms) ?? DialectMatrix.BaselineVersion[dbms]) >= floor)
            {
                continue;
            }

            (names ??= []).Add(TargetDbmsNames.Display(dbms));
        }

        return names is null ? null : TargetDbmsNames.JoinDisplayNames(names);
    }

    // Name-filter first, like AnalyzeContextRules — only the DateTimePart
    // consumers below pay for target-set resolution.
    private static void AnalyzeDatepartValidity(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("Extract" or "Datepart" or "Dateadd" or "Datediff"
                or "DateTrunc" or "Datetrunc" or "Interval" or "Timestampadd" or "Timestampdiff")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        DialectTargetSet targets = GetTargets(context, cache);
        if (targets.IsEmpty)
        {
            return;
        }

        DatepartValidityRule.Check(context, invocation, targets);
    }

    // Name-filter first, like AnalyzeDatepartValidity — the row-count names sit
    // on every paginated query's hot path, so they must not pay for a config lookup.
    private static void AnalyzeArgumentValueValidity(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("RegexpCount" or "RegexpInstr" or "RegexpLike"
                or "RegexpReplace" or "RegexpSubstr" or "FetchFirst" or "FetchNext" or "Limit"
                or "Top" or "GroupBy")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        DialectTargetSet targets = GetTargets(context, cache);
        if (targets.IsEmpty)
        {
            return;
        }

        ArgumentValueValidityRule.Check(context, invocation, targets);
    }

    // IsNull / IsNotNull are SqlExpression properties, so the column under test is
    // the receiver. Gated on a configured target set like every other rule, though
    // the verdict itself is dialect-independent.
    private static void AnalyzeSchemaNullability(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
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
    private static void AnalyzeNotInSubquery(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != "NotIn"
            || invocation.Arguments.Length != 1
            || invocation.TargetMethod.Parameters[0].Type
                .ToDisplayString() != "SqlArtisan.ISubquery"
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

    // The explicit-column-list overload and the Set form, whose assignments are
    // its column list; the positional form supplies every column by construction,
    // and InsertIgnoreInto asked for failures to be skipped.
    private static void AnalyzeInsertColumns(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        bool columnList = invocation.TargetMethod.Name == "InsertInto"
            && invocation.Arguments.Length == 2;
        IInvocationOperation? setHead = invocation.TargetMethod.Name == "Set"
            ? InsertMissingRequiredColumnRule.BareInsertHead(invocation)
            : null;
        if (!columnList && setHead is null)
        {
            return;
        }

        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        if (columnList)
        {
            InsertMissingRequiredColumnRule.Check(context, invocation);
        }
        else
        {
            InsertMissingRequiredColumnRule.CheckSet(context, invocation, setHead!);
        }
    }

    // Count(Asterisk) shares the arity, and COUNT(DISTINCT col) is asking for
    // values by construction, so only the plain object overload is a candidate.
    private static void AnalyzeCountArgument(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
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

    // Like carries the column as its receiver; a wrapping call carries it as an
    // argument, so the two enter the rule by different doors.
    private static void AnalyzeIndexedColumnFilter(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
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
    private static void AnalyzeCorrelatedDml(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("Update" or "DeleteFrom" or "MergeInto")
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

    private static void AnalyzeCSharpFallback(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        var binary = (IBinaryOperation)context.Operation;
        CSharpFallbackRule.CheckReferenceEquality(context, binary);
        CSharpFallbackRule.CheckConcatenation(context, binary);
    }

    private static void AnalyzeInterpolation(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        if (GetTargets(context, cache).IsEmpty)
        {
            return;
        }

        CSharpFallbackRule.CheckInterpolation(
            context, (IInterpolatedStringOperation)context.Operation);
    }

    private static void AnalyzeIdentifierLength(
        OperationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree,
        DialectTargetSet> cache)
    {
        (IMethodSymbol? member, ImmutableArray<IArgumentOperation> arguments) =
            context.Operation switch
            {
                IInvocationOperation invocation => (invocation.TargetMethod, invocation.Arguments),
                IObjectCreationOperation { Constructor: { } constructor } creation => (
                    constructor,
                    creation.Arguments),
                _ => (null, default),
            };

        if (member is null)
        {
            return;
        }

        // A table class lives in the user's assembly but forwards its constructor
        // argument to a SqlArtisan naming base — admitted so the rule can trace it.
        if (!IsFromSqlArtisan(member.ContainingAssembly)
            && !(member.MethodKind == MethodKind.Constructor
                && IdentifierLengthRule.DerivesFromIdentifierBase(member.ContainingType)))
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
        // Keyed like the family loops below: the message names the key, so the
        // same key under two different replacements is two reports, not one.
        var reportedRemovedKeys = new HashSet<(string Key, string Replacement)>();
        var reportedOverrideValues = new HashSet<(string Key, string Value)>();
        string[] overrideKeys = [.. DialectMatrix.AllOverrideKeys.Distinct()];

        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            AnalyzerConfigOptions options =
                context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            // Any value, recognized or not: the key does nothing either way. Blank is
            // unset — the kept CompilerVisibleProperty emits the MSBuild keys to every build.
            foreach (string removedKey in AnalyzerConfigResolver.RemovedKeys)
            {
                if (!AnalyzerConfigResolver.TryGetSetValue(options, removedKey, out _))
                {
                    continue;
                }

                string replacement = AnalyzerConfigResolver.RemovedKeyReplacement(
                    options,
                    removedKey);
                if (reportedRemovedKeys.Add((removedKey, replacement)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RemovedConfigurationKey,
                        Location.None,
                        removedKey,
                        replacement));
                }
            }

            // ResolveOverride honors any construct-prefixed key, so validation sweeps
            // what the options carry; the matrix list is only the no-enumeration fallback.
            IEnumerable<string> candidateOverrideKeys =
                AnalyzerConfigResolver.TryEnumerateConstructKeys(
                    options,
                    out List<string> constructKeys)
                    ? constructKeys
                    : overrideKeys;
            foreach (string overrideKey in candidateOverrideKeys)
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

    // Three more SQLA0001 reasons (#432), each deduplicated at the granularity its
    // message varies by: directory-scoped .editorconfig can give trees different
    // configs, so a coarser dedup key would mute a differing message.
    private static void ValidateSyntaxFamily(CompilationAnalysisContext context)
    {
        var reportedUnrecognizedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reportedSyntaxValues = new HashSet<(string Key, string Value)>();
        bool reportedEmptySet = false;
        string validDbmsNames = string.Join("/", AnalyzerConfigResolver.DbmsNames);

        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            AnalyzerConfigOptions options =
                context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
            bool familyPresent = AnalyzerConfigResolver.IsFamilyPresent(options);

            if (AnalyzerConfigResolver.TryEnumerateSyntaxKeys(options, out List<string> syntaxKeys))
            {
                foreach (string key in syntaxKeys)
                {
                    if (!AnalyzerConfigResolver.IsRecognizedSyntaxKey(key)
                        && reportedUnrecognizedKeys.Add(key))
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
                        "any, none, or a numeric engine version such as 8.0.16, 23, "
                            + "3.44, or 2022"));
                }
            }

            // An unrecognized value already explains this tree's empty set — reporting
            // it again would duplicate one root cause under two descriptors.
            if (familyPresent && !hasUnrecognizedSyntaxValue && !reportedEmptySet
                && AnalyzerConfigResolver.ResolveTargets(options).IsEmpty)
            {
                reportedEmptySet = true;
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ConfigurationDisablesAllDialects, Location.None));
            }
        }
    }

    internal static bool IsFromSqlArtisan(IAssemblySymbol? assembly) =>
        assembly?.Name == SqlArtisanAssemblyName;

    // An enum member or a SqlParameters/SqlStatement member can only match a same-named row
    // by coincidence (DateTimePart.Day, SqlParameters.Count); DateTimePart and RegexpOptions
    // values are SQLA0104's.
    private static bool IsConstructMember(ISymbol member) =>
        IsFromSqlArtisan(member.ContainingAssembly)
        && member.ContainingType.TypeKind != TypeKind.Enum
        && !(member.ContainingType.ContainingNamespace.ToDisplayString() == "SqlArtisan"
            && member.ContainingType.Name is "SqlParameters" or "SqlStatement");

    private static string DisplayName(string memberName, int? arity, bool isArityLevel)
    {
        if (OperatorDisplayName(memberName) is { } operatorName)
        {
            return operatorName;
        }

        if (!isArityLevel || !arity.HasValue)
        {
            return memberName;
        }

        // "declared with N parameters", not "N-argument form": a params overload's
        // declared count exceeds what the call site wrote and would read as a misfire.
        string plural = arity.Value == 1 ? "" : "s";
        return $"{memberName} (overload declared with {arity.Value} parameter{plural})";
    }

    // Users write the C# glyph, not the CLR method name — show "operator %", not "op_Modulus".
    // The override key in the message still derives from the CLR name
    // (sqlartisan_construct_op_modulus).
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
