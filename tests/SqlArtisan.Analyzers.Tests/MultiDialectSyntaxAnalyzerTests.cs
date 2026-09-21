using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The #432 <c>sqlartisan_syntax_*</c> family: what only a multi-DBMS set can
/// exercise (join wording, one diagnostic per failing DBMS, SQLA0001's reasons and
/// SQLA0002); <see cref="DialectUsageAnalyzerTests"/> covers the single-DBMS case.
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
                "sqlartisan_syntax_oracle", "tru", "any, none, or a numeric engine version such as "
                    + "8.0.16, 23, 3.44, or 2022"));

        await test.RunAsync();
    }

    // Reported per key, not per value: the same typo migrated from the MSBuild
    // property into .editorconfig is two mistakes, each named (release audit pass 7).
    [Fact]
    public async Task LegacyDbmsSameJunkOnBothSurfaces_ReportsBothKeys()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanTargetDbms = postgres
            build_property.SqlArtisanTargetVersion = 16
            """;
        const string editorConfig = """
            root = true
            [*.cs]
            sqlartisan_target_dbms = postgres
            """;

        const string expected = "one of: mysql/oracle/postgresql/sqlite/sqlserver";
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("build_property.SqlArtisanTargetDbms", "postgres", expected));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "postgres", expected));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_<dbms> = <version-or-any>"));
        await test.RunAsync();
    }

    // The legacy pair's value validation reads both surfaces too — a junk
    // version through the MSBuild property must not silently resolve to unset
    // (losing SQLA0101 coverage with no visible reason).
    [Fact]
    public async Task LegacyVersionJunkViaMSBuildProperty_ReportsSqla0001()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanTargetDbms = postgresql
            build_property.SqlArtisanTargetVersion = 16 or so
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig: null);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "build_property.SqlArtisanTargetVersion",
                "16 or so",
                "a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_postgresql = any"));

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
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

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
            "In at least one file's effective configuration, every 'sqlartisan_syntax_*' "
                + "key is 'none', "
                + "so that file has no dialect left to check"));

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
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

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
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

        await test.RunAsync();
    }

    // The shipped props declares a CompilerVisibleProperty per DBMS, so the SDK
    // emits all five keys (empty when unset) to every consumer; read as "family
    // present" they hijacked a legacy-configured project's resolution.
    [Fact]
    public async Task BlankFamilyPropertiesBesideLegacyPair_LeaveLegacyResolutionIntact()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanSyntaxMySql =
            build_property.SqlArtisanSyntaxOracle =
            build_property.SqlArtisanSyntaxPostgreSql =
            build_property.SqlArtisanSyntaxSqlite =
            build_property.SqlArtisanSyntaxSqlServer =
            """;

        var test = AnalyzerVerifier.Create(
            RollupUsageTemplate,
            AnalyzerVerifier.LegacyEditorConfig("mysql"));
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_mysql = any"));

        await test.RunAsync();
    }

    // The zero-config half of the same hazard: the analyzer must stay
    // completely silent for a package consumer that configured nothing.
    [Fact]
    public async Task BlankFamilyPropertiesWithNoOtherConfig_StaySilent()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanSyntaxMySql =
            build_property.SqlArtisanSyntaxOracle =
            build_property.SqlArtisanSyntaxPostgreSql =
            build_property.SqlArtisanSyntaxSqlite =
            build_property.SqlArtisanSyntaxSqlServer =
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig: null);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));

        await test.RunAsync();
    }

    // Family-wins-outright (#432's precedence rule): the legacy pair's DBMS is
    // dropped, not merged in, and the coexistence report — not SQLA0002 — names
    // exactly which one.
    [Fact]
    public async Task LegacyAndFamilyCoexist_ReportsSqla0001NamingDroppedDbms_NeverSqla0002()
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
            .WithArguments(
                "sqlartisan_target_dbms",
                "postgresql",
                "PostgreSQL",
                "sqlartisan_syntax_postgresql = any"));

        await test.RunAsync();
    }

    // The report names the surface actually read: a project setting only the
    // MSBuild property never wrote the .editorconfig line the message would
    // otherwise claim is ignored.
    [Fact]
    public async Task LegacyDbmsViaMSBuildPropertyAndFamilyCoexist_ReportNamesTheMSBuildKey()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanTargetDbms = postgresql
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = any
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "build_property.SqlArtisanTargetDbms",
                "postgresql",
                "PostgreSQL",
                "sqlartisan_syntax_postgresql = any"));

        await test.RunAsync();
    }

    // The suggestion carries the legacy version over: a bare key would remediate
    // nothing (blank reads as unset), and `= any` would silently shed the
    // dialect's SQLA0101 coverage.
    [Fact]
    public async Task LegacyPairWithVersionAndFamilyCoexist_SuggestionCarriesTheVersion()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_target_version = 16
            sqlartisan_syntax_oracle = any
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_target_dbms",
                "postgresql",
                "PostgreSQL",
                "sqlartisan_syntax_postgresql = 16"));

        await test.RunAsync();
    }

    // Mid-migration: the family names the same DBMS the legacy pair does, so
    // nothing is dropped — a coexistence report would claim an unchecked dialect
    // while an SQLA0101 in the same build proves it checked.
    [Fact]
    public async Task LegacyAndFamilySameDbms_ReportsTheDialectDiagnosticOnly()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
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

        await test.RunAsync();
    }

    // `none` for the legacy pair's own DBMS is the user's explicit statement
    // about it — not a silent drop — so only the empty-set reason reports.
    [Fact]
    public async Task LegacyDbmsSetToNoneInFamily_ReportsEmptySetOnly_NoCoexistenceReport()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_syntax_postgresql = none
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001").WithMessage(
            "In at least one file's effective configuration, every 'sqlartisan_syntax_*' "
                + "key is 'none', "
                + "so that file has no dialect left to check"));

        await test.RunAsync();
    }

    // An invalid family value for the legacy pair's own DBMS: the
    // value-validation reason already names the key and the value, so neither
    // the coexistence report nor SQLA0002 fires on top of it.
    [Fact]
    public async Task LegacyDbmsWithInvalidFamilyValueForSameDbms_ReportsValueValidationOnly()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            sqlartisan_syntax_postgresql = tru
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_postgresql",
                "tru",
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

        await test.RunAsync();
    }

    // The pitfall docs/analyzer.md documents for the legacy pair ("a version
    // alone identifies no engine") still earns the deprecation nag: the key
    // itself resolves even though it has no dialect effect.
    [Fact]
    public async Task LegacyVersionAlone_NoFamilyPresent_ReportsSqla0002()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_target_version = 16
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_<dbms> = <version-or-any>"));

        await test.RunAsync();
    }

    [Fact]
    public async Task LegacyPairAlone_ResolvesCorrectlyAndReportsSqla0002()
    {
        string editorConfig = AnalyzerVerifier.LegacyEditorConfig("mysql");

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_mysql = any"));

        await test.RunAsync();
    }

    // Like SQLA0001, SQLA0002 carries no file location, so only a global analyzer
    // config reaches it (docs/analyzer.md's Migrating section); the file-scoped
    // twin below shows an .editorconfig severity line leaving it standing.
    [Fact]
    public async Task GlobalConfigSuppressesSqla0002_ButLeavesSqla0100Active()
    {
        string editorConfig = AnalyzerVerifier.LegacyEditorConfig("mysql");
        const string globalConfig = """
            is_global = true
            dotnet_diagnostic.SQLA0002.severity = none
            """;

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task FileScopedSeverityDoesNotReachSqla0002()
    {
        string editorConfig = AnalyzerVerifier.LegacyEditorConfig("mysql")
            + "\ndotnet_diagnostic.SQLA0002.severity = none\n";

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_mysql = any"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

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

    // Directory-scoped .editorconfig files are an advertised shape, so the
    // dedup must key on the message content, not one compilation-wide flag —
    // a coarser key mutes the second directory's differing suggestion.
    [Fact]
    public async Task TwoDirectoriesWithDifferentLegacyConfigs_ReportSqla0002ForEach()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_target_dbms = postgresql
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_target_dbms = oracle
            sqlartisan_target_version = 21
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", SecondaryUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_postgresql = any"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0002")
            .WithArguments("sqlartisan_syntax_oracle = 21"));

        await test.RunAsync();
    }

    [Fact]
    public async Task TwoDirectoriesWithDifferentDroppedVersions_ReportSqla0001ForEach()
    {
        const string rootConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = any
            sqlartisan_target_dbms = postgresql
            sqlartisan_target_version = 15
            """;
        const string subConfig = """
            [*.cs]
            sqlartisan_target_version = 16
            """;

        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(RollupUsageTemplate),
            rootConfig);
        test.TestState.Sources.Add(("/sub/Second.cs", SecondaryUsageSource));
        test.TestState.AnalyzerConfigFiles.Add(("/sub/.editorconfig", subConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_target_dbms",
                "postgresql",
                "PostgreSQL",
                "sqlartisan_syntax_postgresql = 15"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_target_dbms",
                "postgresql",
                "PostgreSQL",
                "sqlartisan_syntax_postgresql = 16"));

        await test.RunAsync();
    }
}
