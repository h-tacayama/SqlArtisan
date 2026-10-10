using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The #432 <c>sqlartisan_syntax_*</c> family: what only a multi-DBMS set can
/// exercise (join wording, one diagnostic per failing DBMS) and SQLA0001's reasons;
/// <see cref="DialectUsageAnalyzerTests"/> covers the single-DBMS case.
/// </summary>
public class MultiDialectSyntaxAnalyzerTests
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

    private const string ConcatArityUsageTemplate = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class C
        {
            void M()
            {
                var x = {|#0:Concat("a", "b", "c")|};
            }
        }
        """;

    private const string ExceptUsageTemplate = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T() : base("t", string.Empty) { }
        }

        class C
        {
            void M()
            {
                T t = new();
                var x = {|#0:Select(t.Asterisk).From(t).Except|}.Select(t.Asterisk).From(t);
            }
        }
        """;

    private const string MergeIntoUsageTemplate = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T() : base("t", string.Empty) { }
        }

        class C
        {
            void M()
            {
                T t = new();
                var x = {|#0:MergeInto(t)|};
            }
        }
        """;

    private static string AliasUsage(string alias) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class C
        {
            void M()
            {
                var x = Bind(1).As({|#0:"{{alias}}"|});
            }
        }
        """;

    // Rollup is unsupported on MySQL and SQLite but supported on Oracle in the
    // shipped matrix — proves a supported member of the set is left out of the
    // join and only one diagnostic is reported (not one per configured DBMS).
    [Fact]
    public async Task Sqla0100_SetHasMultipleUnsupportedDbms_JoinsThemIntoOneDiagnostic()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_oracle = any
            sqlartisan_syntax_sqlite = any
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100")
            .WithLocation(0)
            .WithArguments("Rollup", "MySQL and SQLite", "sqlartisan_construct_rollup"));

        await test.RunAsync();
    }

    // Except is version-bound on MySQL (8.0.31) and Oracle (21) but unbounded on
    // PostgreSQL: SQLA0101 reports once per failing DBMS (unlike SQLA0100's join),
    // so two reports here and none for PostgreSQL.
    [Fact]
    public async Task Sqla0101_SetHasMultipleVersionBoundDbms_ReportsOnePerFailingDbms()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = 8.0
            sqlartisan_syntax_oracle = 19
            sqlartisan_syntax_postgresql = any
            """;

        var test = AnalyzerVerifier.Create(ExceptUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0101")
            .WithLocation(0)
            .WithArguments("Except", "MySQL", "8.0.31", "8.0", "sqlartisan_construct_except"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0101")
            .WithLocation(0)
            .WithArguments("Except", "Oracle", "21", "19", "sqlartisan_construct_except"));

        await test.RunAsync();
    }

    // MergeInto fails two ways across the set (#432's example): MySQL has no MERGE
    // entry (SQLA0100), PostgreSQL supports it only from 15 (SQLA0101); both facts
    // are actionable, so both are reported.
    [Fact]
    public async Task ConstructFailsTwoWaysAcrossTheSet_ReportsBothSqla0100AndSqla0101()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_postgresql = 14
            """;

        var test = AnalyzerVerifier.Create(MergeIntoUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100")
            .WithLocation(0)
            .WithArguments("MergeInto", "MySQL", "sqlartisan_construct_merge_into"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0101")
            .WithLocation(0)
            .WithArguments(
                "MergeInto",
                "PostgreSQL",
                "15",
                "14",
                "sqlartisan_construct_merge_into"));

        await test.RunAsync();
    }

    // The override is the user's claim about their configuration — dialect-
    // independent, resolved once per usage (ADR 0008, #432): forcing Rollup
    // unsupported across three dialects reports one diagnostic naming all three.
    [Fact]
    public async Task ConstructOverride_ResolvedOnceAcrossTheSet_NotDuplicatedPerDbms()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_oracle = any
            sqlartisan_syntax_sqlite = any
            sqlartisan_construct_rollup = unsupported
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100")
            .WithLocation(0)
            .WithArguments("Rollup", "MySQL, Oracle and SQLite", "sqlartisan_construct_rollup"));

        await test.RunAsync();
    }

    // SQLA0103's limit and unit are per-dialect (PostgreSQL: 63 bytes, SQL
    // Server: 128 characters) so — like SQLA0101 — the failing dialects cannot
    // join into one message; a 130-character alias exceeds both.
    [Fact]
    public async Task Sqla0103_SetHasMultipleOverLimitDbms_ReportsOnePerFailingDbms()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_postgresql = any
            sqlartisan_syntax_sqlserver = any
            """;

        var test = AnalyzerVerifier.Create(AliasUsage(new string('a', 130)), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103")
            .WithLocation(0)
            .WithArguments(new string('a', 130), "PostgreSQL", 63, "bytes"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103")
            .WithLocation(0)
            .WithArguments(new string('a', 130), "SQL Server", 128, "characters"));

        await test.RunAsync();
    }

    // SQLA0102 pairs each trigger with the one dialect whose grammar restricts it
    // (#264) — "is this dialect in the set", not "is it the target"; PostgreSQL
    // supports Limit outright, so the diagnostic fires once, for MySQL.
    [Fact]
    public async Task Sqla0102_MySqlInASetWithPostgreSql_StillReports()
    {
        const string source = """
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public T() : base("t", "") { Id = new DbColumn(this, "id"); }
            }

            class C
            {
                void M()
                {
                    T t = new T();
                    T s = new T();
                    var q = Select(t.Id).From(t).Where(
                        t.Id.In({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|}));
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_postgresql = any
            """;

        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task UnrecognizedSyntaxKeyName_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_postgres = 16
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_postgres",
                "mysql/oracle/postgresql/sqlite/sqlserver"));

        await test.RunAsync();
    }

    [Fact]
    public async Task UnrecognizedSyntaxValue_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = tru
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_oracle", "tru", "any, none, or an Oracle version such as 23"));

        await test.RunAsync();
    }

    // A removed key is reported whatever its value — it configures nothing either way —
    // once per key, the replacement spelled for the surface the key was set on (#654).
    [Fact]
    public async Task RemovedKeysOnBothSurfaces_ReportEachKeyInItsOwnSurfaceSyntax()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanTargetDbms = postgresql
            build_property.SqlArtisanTargetVersion = 16
            """;
        const string editorConfig = """
            root = true
            [*.cs]
            sqlartisan_target_dbms = postgres
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("build_property.SqlArtisanTargetDbms", "<SqlArtisanSyntax<Dbms>>"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("build_property.SqlArtisanTargetVersion", "<SqlArtisanSyntax<Dbms>>"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithMessage("'sqlartisan_target_dbms' was removed and is ignored; delete it, and "
                + "declare each dialect with 'sqlartisan_syntax_<dbms>' where not already "
                + "declared"));
        await test.RunAsync();
    }

    // Non-letter junk after the digits is a typo, not a release-name suffix like
    // 23ai — it must hit the value check, not silently resolve to its digits.
    [Fact]
    public async Task SyntaxValueWithTrailingJunk_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_postgresql = 14!!
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_postgresql",
                "14!!",
                "any, none, or a PostgreSQL version such as 16"));

        await test.RunAsync();
    }

    [Fact]
    public async Task FamilyPresentButEveryKeyIsNone_ReportsSqla0001()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = none
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithMessage(
            "Every 'sqlartisan_syntax_*' key is 'none' wherever one is set, so no file has a "
                + "dialect left to check"));

        await test.RunAsync();
    }

    // A single invalid-valued family key already explains the empty set (the
    // value-validation reason above) — reporting the empty-set reason too would
    // duplicate the same root cause under two SQLA0001 reports.
    [Fact]
    public async Task InvalidValuedFamilyKey_ReportsOnlyValueValidation_NotAlsoEmptySet()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = tru
            """;

        // Asserted by arguments, not just by id: the empty-set reason shares
        // SQLA0001, so a bare CompilerWarning("SQLA0001") would pass against
        // either message and prove nothing about which one won the dedup.
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_oracle",
                "tru",
                "any, none, or an Oracle version such as 23"));

        await test.RunAsync();
    }

    // The same bad value through the MSBuild surface must report the same
    // value-validation reason — reading only the .editorconfig key once left the
    // set empty and reported "every key is 'none'".
    [Fact]
    public async Task InvalidValuedFamilyProperty_ReportsValueValidation_NotEmptySet()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanSyntaxOracle = tru
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig: null);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "build_property.SqlArtisanSyntaxOracle",
                "tru",
                "any, none, or an Oracle version such as 23"));

        await test.RunAsync();
    }

    // The shipped props declares a CompilerVisibleProperty per family DBMS and per removed
    // key, so the SDK emits every one of them (empty when unset) to every consumer: the
    // analyzer must stay completely silent for a package consumer that configured nothing.
    [Fact]
    public async Task BlankDeclaredPropertiesWithNoOtherConfig_StaySilent()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanSyntaxMySql =
            build_property.SqlArtisanSyntaxOracle =
            build_property.SqlArtisanSyntaxPostgreSql =
            build_property.SqlArtisanSyntaxSqlite =
            build_property.SqlArtisanSyntaxSqlServer =
            build_property.SqlArtisanTargetDbms =
            build_property.SqlArtisanTargetVersion =
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig: null);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));

        await test.RunAsync();
    }

    // The removed key adds nothing to the family: only Oracle is checked (Rollup runs
    // there), and the leftover line is still reported.
    [Fact]
    public async Task RemovedKeyBesideFamily_IsReported_AndAddsNoDialect()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_syntax_oracle = any
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "sqlartisan_syntax_<dbms>"));

        await test.RunAsync();
    }

    // Mid-migration — the family line written, the old one not yet deleted — the family
    // checks PostgreSQL at its own 14 and the dead line is still named.
    [Fact]
    public async Task RemovedKeyBesideFamilyForTheSameDbms_ReportsBoth()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_target_version = 16
            sqlartisan_syntax_postgresql = 14
            """;

        var test = AnalyzerVerifier.Create(MergeIntoUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0101")
            .WithLocation(0)
            .WithArguments(
                "MergeInto",
                "PostgreSQL",
                "15",
                "14",
                "sqlartisan_construct_merge_into"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "sqlartisan_syntax_<dbms>"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_version", "sqlartisan_syntax_<dbms>"));

        await test.RunAsync();
    }

    // Rollup is unsupported on MySQL, yet no SQLA0100: the removed key configures nothing.
    [Fact]
    public async Task RemovedKeyAlone_ChecksNothing_ReportsTheKey()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = mysql
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "sqlartisan_syntax_<dbms>"));

        await test.RunAsync();
    }

    [Fact]
    public async Task ArityLevelEntry_DisplayNameSaysDeclaredParameters()
    {
        // The declared-parameter phrasing is deliberate: at a params call site
        // the declared count exceeds the written argument count, so an
        // "N-argument form" display would read as a misfire.
        var test = AnalyzerVerifier.Create(
            ConcatArityUsageTemplate, AnalyzerVerifier.EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100")
                .WithLocation(0)
                .WithMessage(
                    "'Concat (overload declared with 4 parameters)' is not supported on Oracle. "
                        + "Set 'sqlartisan_construct_concat_arity4 = supported' in .editorconfig "
                        + "if your engine version supports it."));

        await test.RunAsync();
    }

    private const string SecondaryUsageSource = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class D
        {
            void M()
            {
                var x = Rollup("a");
            }
        }
        """;

    private const string NvlUsageSource = """
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class D
        {
            void M()
            {
                var x = Nvl("a", "b");
            }
        }
        """;

    // A path-scoped `none` beside a scope that resolves a dialect is a deliberate carve-out;
    // the sub file's Nvl, unsupported on PostgreSQL, proves the carve-out applied (#655).
    [Fact]
    public async Task NoneCarveOutBesideAResolvedScope_StaysSilent()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_postgresql = 16
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_syntax_postgresql = none
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", NvlUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));

        await test.RunAsync();
    }

    // The root's bad value already explains why nothing is checked; the sub's `none`
    // is not also reported as if every key were `none`.
    [Fact]
    public async Task NoneBesideAnUnrecognizedValue_ReportsOnlyTheValue()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_postgresql = postgres
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_syntax_postgresql = none
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", NvlUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_postgresql",
                "postgres",
                "any, none, or a PostgreSQL version such as 16"));

        await test.RunAsync();
    }

    // A blank in a narrower section replaces the broader value, so it would unset the
    // dialect for those files without a word; `none` is the spelling that says so (#655).
    [Fact]
    public async Task BlankSyntaxKeyInANarrowerSection_ReportsSqla0001()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_sqlite = any
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_syntax_sqlite =
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", NvlUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_sqlite",
                "",
                "any, none, or a SQLite version such as 3.44"));

        await test.RunAsync();
    }

    // SQL Server's bounds are years, so its product number (16 for 2022) would fail every
    // bound, and a year on another engine would clear every one (#655).
    [Theory]
    [InlineData("sqlserver", "16", "any, none, or a SQL Server release year such as 2022")]
    [InlineData("postgresql", "2022", "any, none, or a PostgreSQL version such as 16")]
    public async Task VersionInTheOtherSpelling_ReportsSqla0001(
        string dbms,
        string version,
        string expected)
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            AnalyzerVerifier.EditorConfig(dbms, version));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments($"sqlartisan_syntax_{dbms}", version, expected));

        await test.RunAsync();
    }

    // The reports carry no location, so one key read by files with different family lines
    // must get one piece of advice true for all of them — never "delete it" for the
    // migrated directory beside a replacement line that would override it (#654).
    [Fact]
    public async Task RemovedKeyAcrossDirectoriesWithDifferentFamilies_ReportsOnceUniformly()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_target_version = 16
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_syntax_postgresql = 14
            sqlartisan_target_dbms = oracle
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", SecondaryUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "sqlartisan_syntax_<dbms>"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_version", "sqlartisan_syntax_<dbms>"));

        await test.RunAsync();
    }
}
