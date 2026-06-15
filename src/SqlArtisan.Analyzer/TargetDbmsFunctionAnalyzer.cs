using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzer;

// Opt-in dialect check over the permissive SqlArtisan.Sql API. The target DBMS is
// resolved PER FILE from .editorconfig (sqlartisan_target_dbms), falling back to a
// project-wide MSBuild property (build_property.SqlArtisanTargetDbms). Per-file
// resolution is what lets one project pin different DBMS to different folders via
// .editorconfig globs. It NEVER blocks: Warning severity, and functions absent
// from the catalog are ignored (permissive default).
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetDbmsFunctionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SQLA0001";

    // .editorconfig key (per file/folder) and MSBuild fallback (project-wide).
    private const string EditorConfigKey = "sqlartisan_target_dbms";
    private const string MsBuildKey = "build_property.SqlArtisanTargetDbms";
    private const string SqlFacadeType = "SqlArtisan.Sql";

    // Minimal numeric matrix (the opt-in catalog), case-insensitive on DBMS name.
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
        description: "Set sqlartisan_target_dbms (.editorconfig) or <SqlArtisanTargetDbms> to flag unsupported functions.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            AnalyzerConfigOptionsProvider provider = start.Options.AnalyzerConfigOptionsProvider;
            start.RegisterOperationAction(ctx => Analyze(ctx, provider), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, AnalyzerConfigOptionsProvider provider)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;

        if (method.ContainingType?.ToDisplayString() != SqlFacadeType
            || !Catalog.TryGetValue(method.Name, out ImmutableHashSet<string>? supported))
        {
            return;
        }

        string? target = ResolveTarget(provider, invocation.Syntax.SyntaxTree);
        if (string.IsNullOrWhiteSpace(target)   // opt-in: silent for files with no target
            || supported.Contains(target!))     // case-insensitive
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            method.Name,
            target,
            string.Join(", ", supported.OrderBy(s => s, StringComparer.Ordinal))));
    }

    // Per-file .editorconfig wins; project-wide MSBuild property is the fallback.
    private static string? ResolveTarget(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        if (provider.GetOptions(tree).TryGetValue(EditorConfigKey, out string? perFile)
            && !string.IsNullOrWhiteSpace(perFile))
        {
            return perFile.Trim();
        }

        if (provider.GlobalOptions.TryGetValue(MsBuildKey, out string? global)
            && !string.IsNullOrWhiteSpace(global))
        {
            return global.Trim();
        }

        return null;
    }

    private static ImmutableHashSet<string> Dbms(params string[] values) =>
        ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, values);
}
