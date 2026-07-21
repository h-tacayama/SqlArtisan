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
    public async Task UnsupportedConstructForTarget_ReportsSqla0002()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task MemberOverrideSupported_SilencesSqla0002()
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
    public async Task MemberOverrideUnsupported_ForcesSqla0002OnOtherwiseSupportedDialect()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            sqlartisan_construct_rollup = unsupported
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidTargetValue_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgres
            """;

        string source = RollupUsageTemplate.Replace("{|#0:", string.Empty).Replace("|}", string.Empty);
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001"));

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidOverrideValue_ReportsSqla0001AlongsideSqla0002()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            sqlartisan_construct_rollup = maybe
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001"));

        await test.RunAsync();
    }

    [Fact]
    public async Task StringAggThreeArgForm_OnSqlServer_ReportsSqla0002ButTwoArgFormDoesNot()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task MatchOverloads_OnTargetWhereNeitherIsSupported_BothReportSqla0002()
    {
        // Real matrix union entry (not synthetic): MySQL's Match(object, params object[]) and
        // SQLite's Match(DbTableBase, object) both declare 2 parameters, so they collapse into
        // one MatrixKey — on a target where neither dialect applies, both correctly warn.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var table = new DbTable("posts", "p");
                    var mysqlShaped = {|#0:Match("col1", "col2")|};
                    var sqliteShaped = {|#1:Match(table, "pattern")|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(1));

        await test.RunAsync();
    }

    [Fact]
    public async Task MatchOverloads_OnTargetWhereOnlyOneIsSupported_UnionMeansNeitherWarns()
    {
        // The documented false-negative trade-off (DialectMatrix.cs's "key collision caveat"):
        // targeting MySQL, the SQLite-shaped overload is NOT actually valid, but the matrix
        // cannot tell the two overloads apart by arity, so it stays silent rather than guess —
        // this test locks in that accepted behavior so a future edit can't silently tighten or
        // loosen it without the change being visible here.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var table = new DbTable("posts", "p");
                    var mysqlShaped = Match("col1", "col2");
                    var sqliteShaped = Match(table, "pattern");
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);

        await test.RunAsync();
    }

    [Fact]
    public async Task SequenceNextvalProperty_OnOracle_StaysSilent()
    {
        // Regression for a fixed false positive: Sequence("s").Nextval is Oracle's form, but the
        // DbSequence.Nextval property shares its name with Sql.Nextval("s") (PostgreSQL's form),
        // and the entry was originally PostgreSQL-only — warning on the correct Oracle usage.
        // The entry is now the union of both forms' dialects.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = Sequence("users_id_seq").Nextval;
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task RegexpLike_OnPostgreSql_StaysSilent()
    {
        // Regression for a fixed false positive: the XML docs say "Oracle syntax" only, but
        // PostgreSQL 15 added regexp_like with the same signature — the PostgreSQL 16 baseline
        // supports it, so the original Oracle-only entry warned on valid PostgreSQL usage.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = RegexpLike("name", "^A.*");
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task RoundOneArgForm_OnSqlServer_ReportsSqla0002ButTwoArgFormDoesNot()
    {
        // Real matrix arity split: T-SQL's ROUND requires 2-3 arguments, so the 1-arg overload
        // is invalid on SQL Server while the 2-arg overload is universal.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var ok = Round("price", 2);
                    var bad = {|#0:Round("price")|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = sqlserver
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task BindArrayAndUnnest_OnMySql_ReportSqla0002()
    {
        // The Any/All/Some keys stay the subquery-form union (arity-1 collision, see
        // DialectMatrix), so off-PG the array form is flagged through BindArray/Unnest.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var bind = {|#0:BindArray(new[] { 1, 2 })|};
                    var rows = {|#1:Unnest(bind)|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(1));

        await test.RunAsync();
    }

    [Fact]
    public async Task BindArrayAndUnnest_OnPostgreSql_StaySilent()
    {
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var bind = BindArray(new[] { 1, 2 });
                    var rows = Unnest(bind);
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);

        await test.RunAsync();
    }

    [Fact]
    public async Task DualProperty_UnsupportedTarget_ReportsSqla0002()
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
        // PostgreSQL: FROM DUAL is invalid there (MySQL, by contrast, allows it).
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ModulusOperator_OnOracleTarget_ReportsSqla0002()
    {
        // Overloaded operators reach the analyzer as binary operations (#219): Oracle has no
        // % arithmetic operator (its spelling is MOD(n, m)).
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = {|#0:Abs(1) % 2|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);

        // Locks the operator display mapping (the C# glyph, not "op_Modulus") and the
        // member-level override key the message names — neither is asserted anywhere else.
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithLocation(0)
            .WithArguments("operator %", "Oracle", "sqlartisan_construct_op_modulus"));

        await test.RunAsync();
    }

    [Fact]
    public async Task ModulusOperator_OnSqlServerTarget_StaysSilent()
    {
        // The deliberate mirror of the Mod entry: T-SQL rejects MOD() but accepts % — proves
        // op_Modulus and Mod are independent entries.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = Abs(1) % 2;
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = sqlserver
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task AdditionOperator_OnOracleTarget_StaysSilent()
    {
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = Abs(1) + 2;
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task ModulusOperator_MemberOverrideSupported_SilencesSqla0002()
    {
        // Proves the CLR-name-derived override key (op_Modulus -> sqlartisan_construct_op_modulus)
        // round-trips through the resolver.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var x = Abs(1) % 2;
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            sqlartisan_construct_op_modulus = supported
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        await test.RunAsync();
    }

    [Fact]
    public async Task ModulusCompoundAssignment_OnOracleTarget_ReportsSqla0002()
    {
        // e %= 2 compiles (ModulusOperator derives from SqlExpression) and reaches Roslyn as a
        // compound assignment, not a binary operation — a separate registration.
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    SqlExpression e = Abs(1);
                    {|#0:e %= 2|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002").WithLocation(0));

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
