using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class DialectUsageAnalyzerTests
{
    private const string RollupUsageTemplate = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class C
        {
            void M()
            {
                var x = {|#0:Rollup("a")|};
            }
        }
        """;

    [Fact]
    public async Task NoTargetConfigured_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(RollupUsageTemplate.Replace("{|#0:", string.Empty).Replace("|}", string.Empty));
        await test.RunAsync();
    }

    [Fact]
    public async Task UnsupportedConstructForTarget_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task MemberOverrideSupported_SilencesSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            sqlartisan_construct_rollup = supported
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate.Replace("{|#0:", string.Empty).Replace("|}", string.Empty), editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task MemberOverrideUnsupported_ForcesSqla0001OnOtherwiseSupportedDialect()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            sqlartisan_construct_rollup = unsupported
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidTargetValue_ReportsSqla0002()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgres
            """;

        string source = RollupUsageTemplate.Replace("{|#0:", string.Empty).Replace("|}", string.Empty);
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002"));

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidOverrideValue_ReportsSqla0002AlongsideSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            sqlartisan_construct_rollup = maybe
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002"));

        await test.RunAsync();
    }

    [Fact]
    public async Task StringAggThreeArgForm_OnSqlServer_ReportsSqla0001ButTwoArgFormDoesNot()
    {
        // Real matrix arity split (not synthetic): StringAgg's 2-arg form is PostgreSQL + SQL
        // Server, but the 3-arg inline-ORDER-BY form is PostgreSQL-only — SQL Server orders via
        // the separate .WithinGroup(...) chain instead.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var ok = StringAgg("name", ", ");
                    var bad = {|#0:StringAgg("name", ", ", OrderBy("name"))|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = sqlserver
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task DualProperty_UnsupportedTarget_ReportsSqla0001()
    {
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = {|#0:Dual|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithLocation(0));

        await test.RunAsync();
    }

    // A true multi-file "different .editorconfig section per file, one compilation" scenario is
    // NOT exercised end-to-end here: Microsoft.CodeAnalysis.Testing 1.1.2's AnalyzerConfigFiles
    // only attaches to the primary TestCode document — a second file added via
    // TestState.Sources.Add never receives it (verified empirically: even an unscoped [*.cs]
    // section fails to apply to an additional source, regardless of whether TestCode is also
    // set). The per-tree resolution this would exercise is proven at the right granularity
    // instead: DialectUsageAnalyzer always resolves options via
    // context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree) — Roslyn's standard,
    // genuinely-per-tree API (the same one every directory-scoped analyzer, e.g.
    // StyleCop.Analyzers, relies on) — and AnalyzerConfigResolverTests /
    // DialectSupportResolverTests already cover that resolution logic directly against
    // AnalyzerConfigOptions instances with differing values.
}
