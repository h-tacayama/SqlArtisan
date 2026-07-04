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
        DiagnosticDescriptors.UnsupportedDialectConstruct,
        DiagnosticDescriptors.InvalidConfiguration);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
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

    private static void AnalyzeUsage(OperationAnalysisContext context, string memberName, int? arity)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Operation.Syntax.SyntaxTree);
        if (AnalyzerConfigResolver.ResolveTarget(options) is not { } target)
        {
            return;
        }

        if (DialectSupportResolver.Resolve(options, memberName, arity, target) is not { IsSupported: false } result)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UnsupportedDialectConstruct,
            context.Operation.Syntax.GetLocation(),
            DisplayName(memberName, arity, result.IsArityLevel),
            DisplayName(target),
            DialectMatrix.VerifiedAgainstVersion[target],
            result.OverrideKeyHint));
    }

    private static void ValidateConfiguration(CompilationAnalysisContext context)
    {
        var reportedTargetValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    validTargetNames));
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

    private static bool IsFromSqlArtisan(IAssemblySymbol? assembly) => assembly?.Name == SqlArtisanAssemblyName;

    private static string DisplayName(string memberName, int? arity, bool isArityLevel) =>
        isArityLevel && arity.HasValue ? $"{memberName} ({arity.Value}-argument form)" : memberName;

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
