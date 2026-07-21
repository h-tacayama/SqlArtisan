using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Warns when a SqlArtisan construct is used against a configured target
/// dialect it is not supported on (#93 / ADR 0003). Silent until
/// <c>sqlartisan_target_dbms</c> is set; only ever warns about constructs the
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
        DiagnosticDescriptors.UnsupportedDialectConstruct,
        DiagnosticDescriptors.ContextRestrictedConstruct,
        DiagnosticDescriptors.IdentifierTooLong,
        DiagnosticDescriptors.CorrelatedDmlTargetNotAliased,
        DiagnosticDescriptors.VersionBoundConstruct);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeCompoundAssignment, OperationKind.CompoundAssignment);
        context.RegisterOperationAction(AnalyzeIdentifierLength, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeIdentifierLength, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeContextRules, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeCorrelatedDml, OperationKind.Invocation);
        context.RegisterCompilationAction(ValidateConfiguration);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        IMethodSymbol method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (!IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, method.Name, method.Parameters.Length);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        IPropertySymbol property = ((IPropertyReferenceOperation)context.Operation).Property;
        if (!IsFromSqlArtisan(property.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, property.Name, arity: null);
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        IFieldSymbol field = ((IFieldReferenceOperation)context.Operation).Field;
        if (!IsFromSqlArtisan(field.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, field.Name, arity: null);
    }

    // Overloaded C# operators (#219) reach Roslyn as Binary / CompoundAssignment operations,
    // never as invocations; OperatorMethod is null for built-in operators.
    private static void AnalyzeBinaryOperator(OperationAnalysisContext context)
    {
        if (((IBinaryOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeCompoundAssignment(OperationAnalysisContext context)
    {
        if (((ICompoundAssignmentOperation)context.Operation).OperatorMethod is not { } method
            || !IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        AnalyzeUsage(context, method.Name, method.Parameters.Length);
    }

    private static void AnalyzeUsage(OperationAnalysisContext context, string memberName, int? arity)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);
        if (AnalyzerConfigResolver.ResolveTarget(options) is not { } target)
        {
            return;
        }

        EngineVersion? targetVersion = AnalyzerConfigResolver.ResolveTargetVersion(options);
        if (DialectSupportResolver.Resolve(options, memberName, arity, target, targetVersion) is not { IsSupported: false } result)
        {
            return;
        }

        if (result.IsVersionBound)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.VersionBoundConstruct,
                context.Operation.Syntax.GetLocation(),
                DisplayName(memberName, arity, result.IsArityLevel),
                DisplayName(target),
                result.RequiredVersion,
                targetVersion,
                result.OverrideKeyHint));
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UnsupportedDialectConstruct,
            context.Operation.Syntax.GetLocation(),
            DisplayName(memberName, arity, result.IsArityLevel),
            DisplayName(target),
            result.OverrideKeyHint));
    }

    // Both shipped context rules (#264) are MySQL facts; name-filter first so
    // config resolution is paid only on trigger names.
    private static void AnalyzeContextRules(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        string name = invocation.TargetMethod.Name;
        if (name is not ("Limit" or "Grouping")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);
        if (AnalyzerConfigResolver.ResolveTarget(options) is not TargetDbms.MySql)
        {
            return;
        }

        string dialectName = DisplayName(TargetDbms.MySql);
        if (name == "Limit")
        {
            ContextRules.CheckLimitInQuantifiedSubquery(context, invocation, dialectName);
        }
        else
        {
            ContextRules.CheckGroupingRequiresWithRollup(context, invocation, dialectName);
        }
    }

    // Both DML heads (#256) — the static Sql members and the WithBuilder instance
    // methods — share the name and the DbTableBase-first-parameter shape.
    private static void AnalyzeCorrelatedDml(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is not ("Update" or "DeleteFrom")
            || !IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
        {
            return;
        }

        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);
        if (AnalyzerConfigResolver.ResolveTarget(options) is null)
        {
            return;
        }

        CorrelatedDmlRule.Check(context, invocation);
    }

    private static void AnalyzeIdentifierLength(OperationAnalysisContext context)
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

        if (ResolveIdentifierLimit(context) is not { } resolved)
        {
            return;
        }

        IdentifierLengthRule.Check(context, member, arguments, resolved.Limit, resolved.DialectName);
    }

    private static (DialectIdentifierLimit Limit, string DialectName)? ResolveIdentifierLimit(
        OperationAnalysisContext context)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);
        if (AnalyzerConfigResolver.ResolveTarget(options) is not { } target)
        {
            return null;
        }

        if (IdentifierLengthLimits.For(target) is not { } limit)
        {
            return null;
        }

        return (limit, DisplayName(target));
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
    }

    internal static bool IsFromSqlArtisan(IAssemblySymbol? assembly) => assembly?.Name == SqlArtisanAssemblyName;

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

    private static string DisplayName(TargetDbms target) => target switch
    {
        TargetDbms.MySql => "MySQL",
        TargetDbms.Oracle => "Oracle",
        TargetDbms.PostgreSql => "PostgreSQL",
        TargetDbms.Sqlite => "SQLite",
        TargetDbms.SqlServer => "SQL Server",
        _ => target.ToString(),
    };
}
