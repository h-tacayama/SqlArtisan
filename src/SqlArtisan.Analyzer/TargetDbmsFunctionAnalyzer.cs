using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzer;

// Opt-in dialect checks over the permissive SqlArtisan API. The target DBMS is
// resolved per file from .editorconfig (sqlartisan_target_dbms), falling back to
// the project-wide MSBuild property (build_property.SqlArtisanTargetDbms). Three
// diagnostics, all Warning, all silent until a target is configured:
//   SQLA0001 — function/verb not available on the target DBMS
//   SQLA0002 — the configured target DBMS value is not recognised
//   SQLA0003 — function needs more arguments on the target DBMS (e.g. SS ROUND)
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetDbmsFunctionAnalyzer : DiagnosticAnalyzer
{
    public const string UnsupportedId = "SQLA0001";
    public const string UnknownTargetId = "SQLA0002";
    public const string ArityId = "SQLA0003";

    private const string EditorConfigKey = "sqlartisan_target_dbms";
    private const string MsBuildKey = "build_property.SqlArtisanTargetDbms";
    private const string RootNamespace = "SqlArtisan";

    // Accepted target values (case-insensitive) → canonical DBMS name. Anything
    // not here triggers SQLA0002 instead of a flood of false SQLA0001s.
    private static readonly ImmutableDictionary<string, string> Canonical =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mysql"] = "MySql",
            ["oracle"] = "Oracle",
            ["postgresql"] = "PostgreSql",
            ["postgres"] = "PostgreSql",
            ["pg"] = "PostgreSql",
            ["sqlite"] = "Sqlite",
            ["sqlserver"] = "SqlServer",
            ["mssql"] = "SqlServer",
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    private const string KnownDbms = "MySql, Oracle, PostgreSql, Sqlite, SqlServer";

    // Existence catalog: scalar functions AND clause verbs. Absent names = ignored.
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> Supported =
        new Dictionary<string, ImmutableHashSet<string>>
        {
            ["Abs"] = D("MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer"),
            ["Sign"] = D("MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer"),
            ["Ceil"] = D("MySql", "Oracle", "PostgreSql", "Sqlite"),
            ["Ceiling"] = D("MySql", "PostgreSql", "Sqlite", "SqlServer"),
            ["Trunc"] = D("Oracle", "PostgreSql", "Sqlite"),
            ["Round"] = D("MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer"),
            ["MergeInto"] = D("Oracle", "SqlServer"),
            ["OnConflict"] = D("PostgreSql", "Sqlite"),
            ["OnDuplicateKeyUpdate"] = D("MySql"),
        }.ToImmutableDictionary();

    // Arity rules: (function, canonical DBMS) → minimum argument count required.
    // SQL Server's ROUND(x) is a syntax error — it needs the length argument.
    private static readonly ImmutableDictionary<(string Function, string Dbms), int> MinArgs =
        new Dictionary<(string, string), int>
        {
            [("Round", "SqlServer")] = 2,
        }.ToImmutableDictionary();

    private static readonly DiagnosticDescriptor UnsupportedRule = new(
        UnsupportedId,
        "SQL function not available on the target DBMS",
        "'{0}' is not available on {1}; it is supported on: {2}",
        "SqlArtisan.Dialect",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Set sqlartisan_target_dbms (.editorconfig) or <SqlArtisanTargetDbms> to flag unsupported functions.");

    private static readonly DiagnosticDescriptor UnknownTargetRule = new(
        UnknownTargetId,
        "Unknown SqlArtisan target DBMS",
        "Unknown target DBMS '{0}'; expected one of: " + KnownDbms,
        "SqlArtisan.Dialect",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "sqlartisan_target_dbms / <SqlArtisanTargetDbms> must name a supported DBMS.");

    private static readonly DiagnosticDescriptor ArityRule = new(
        ArityId,
        "SQL function needs more arguments on the target DBMS",
        "'{0}' requires at least {1} argument(s) on {2}",
        "SqlArtisan.Dialect",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Some functions require more arguments on certain DBMS (e.g. ROUND on SQL Server).");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UnsupportedRule, UnknownTargetRule, ArityRule);

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

        if (!IsSqlArtisanMember(method)
            || !Supported.TryGetValue(method.Name, out ImmutableHashSet<string>? supported))
        {
            return;
        }

        string? configured = ResolveTarget(provider, invocation.Syntax.SyntaxTree);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return;   // opt-in: silent until a target is configured
        }

        Location location = invocation.Syntax.GetLocation();

        if (!Canonical.TryGetValue(configured!, out string? dbms))
        {
            context.ReportDiagnostic(Diagnostic.Create(UnknownTargetRule, location, configured));
            return;
        }

        if (!supported.Contains(dbms))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UnsupportedRule, location, method.Name, dbms,
                string.Join(", ", supported.OrderBy(s => s, StringComparer.Ordinal))));
            return;
        }

        if (MinArgs.TryGetValue((method.Name, dbms), out int min) && invocation.Arguments.Length < min)
        {
            context.ReportDiagnostic(Diagnostic.Create(ArityRule, location, method.Name, min, dbms));
        }
    }

    private static bool IsSqlArtisanMember(IMethodSymbol method)
    {
        string? ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        return ns is not null
            && (ns == RootNamespace || ns.StartsWith(RootNamespace + ".", StringComparison.Ordinal));
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

    private static ImmutableHashSet<string> D(params string[] values) =>
        ImmutableHashSet.CreateRange(StringComparer.Ordinal, values);
}
