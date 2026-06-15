using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzer;

// Opt-in dialect check. When the consuming project sets <SqlArtisanTargetDbms>,
// this flags calls to SqlArtisan.Sql functions that the target DBMS does not
// support. It NEVER blocks: severity is Warning, and functions absent from the
// catalog are ignored (permissive default). This is the "(B) permissive API +
// opt-in prevention" layer — no namespace split, no generics, degradable.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetDbmsFunctionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SQLA0001";

    // Set via MSBuild <SqlArtisanTargetDbms>Oracle</SqlArtisanTargetDbms> plus a
    // CompilerVisibleProperty (shipped in the analyzer's build props).
    private const string TargetDbmsOption = "build_property.SqlArtisanTargetDbms";
    private const string SqlFacadeType = "SqlArtisan.Sql";

    // Minimal numeric matrix (the opt-in catalog). Key = function; value = the
    // DBMS that support it. Functions absent here are never flagged.
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> Catalog =
        new Dictionary<string, ImmutableHashSet<string>>
        {
            ["Abs"] = Dbms("MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer"),
            ["Sign"] = Dbms("MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer"),
            ["Ceil"] = Dbms("MySql", "Oracle", "PostgreSql", "Sqlite"),
            ["Ceiling"] = Dbms("MySql", "PostgreSql", "Sqlite", "SqlServer"),
            ["Trunc"] = Dbms("Oracle", "PostgreSql", "Sqlite"),
        }.ToImmutableDictionary();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "SQL function not available on the target DBMS",
        messageFormat: "'{0}' is not available on {1}; it is supported on: {2}",
        category: "SqlArtisan.Dialect",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Set <SqlArtisanTargetDbms> to flag SqlArtisan functions the target DBMS does not support.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            // Opt-in: stay silent unless a target DBMS is configured.
            if (!start.Options.AnalyzerConfigOptionsProvider.GlobalOptions
                    .TryGetValue(TargetDbmsOption, out string? targetDbms)
                || string.IsNullOrWhiteSpace(targetDbms))
            {
                return;
            }

            string target = targetDbms.Trim();
            start.RegisterOperationAction(ctx => Analyze(ctx, target), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, string targetDbms)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;

        if (method.ContainingType?.ToDisplayString() != SqlFacadeType)
        {
            return;
        }

        if (!Catalog.TryGetValue(method.Name, out ImmutableHashSet<string>? supported)
            || supported.Contains(targetDbms))
        {
            return;   // unknown function (permissive) or supported on the target
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            method.Name,
            targetDbms,
            string.Join(", ", supported.OrderBy(s => s))));
    }

    private static ImmutableHashSet<string> Dbms(params string[] values) =>
        values.ToImmutableHashSet();
}
