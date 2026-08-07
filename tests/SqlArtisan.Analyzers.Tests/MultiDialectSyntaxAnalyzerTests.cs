using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The #432 <c>sqlartisan_syntax_*</c> family: set-valued rule behavior (join
/// wording, one diagnostic per failing DBMS, an override resolved once) and
/// the four SQLA0001 configuration-problem reasons plus SQLA0002's
/// deprecation nag. <see cref="DialectUsageAnalyzerTests"/> covers the
/// single-DBMS case (unchanged messages/counts); this file covers what only a
/// multi-DBMS set can exercise.
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

    // Except is version-bound on both MySQL (8.0.31) and Oracle (21) but plain
    // matrix-supported on PostgreSQL with no bound — SQLA0101/0103's cardinality
    // rule (one diagnostic per failing DBMS, unlike SQLA0100's join) means two
    // reports here, and PostgreSQL contributes none.
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

    // MergeInto fails two different ways across the set at once (#432's own
    // example): MySQL has no matrix entry saying it supports MERGE at all
    // (SQLA0100), PostgreSQL supports it but not below 15 (SQLA0101). Dropping
    // either would hide a real, differently-actionable fact — both are reported.
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
            .WithArguments("MergeInto", "PostgreSQL", "15", "14", "sqlartisan_construct_merge_into"));

        await test.RunAsync();
    }

    // The override is the user's own claim about their configuration — dialect-
    // independent, so it is resolved once per usage (ADR 0008, refined by #432),
    // never once per DBMS in the set. Forcing Rollup unsupported across three
    // configured dialects still reports exactly one diagnostic naming all three.
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

    // SQLA0102's context rules pair each trigger with the one dialect whose
    // grammar restricts it (#264) — becoming "is this dialect in the set", not
    // "is it the target". PostgreSQL in the same set supports Limit outright
    // (no matrix or context restriction) and contributes nothing; the
    // diagnostic still fires exactly once, for MySQL.
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
                    var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|}));
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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001"));

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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001"));

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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001"));

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
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "sqlartisan_syntax_oracle",
                "tru",
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

        await test.RunAsync();
    }

    // The same bad value through the MSBuild-property surface must report the
    // same value-validation reason — reading only the .editorconfig key left
    // the resolved set empty and reported "every key is 'none'", which no
    // configuration here says.
    [Fact]
    public async Task InvalidValuedFamilyProperty_ReportsValueValidation_NotEmptySet()
    {
        const string globalConfig = """
            is_global = true
            build_property.SqlArtisanSyntaxOracle = tru
            """;

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig: null);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments(
                "build_property.SqlArtisanSyntaxOracle",
                "tru",
                "any, none, or a numeric engine version such as 8.0.16, 23, 3.44, or 2022"));

        await test.RunAsync();
    }

    // The shipped props declares a CompilerVisibleProperty per DBMS, and the
    // SDK emits every declared property as a key — empty when unset — so these
    // five reach every package consumer. Reading them as "family present"
    // hijacked the resolution: a legacy-configured project lost its SQLA0100
    // and got a coexistence report naming a key it never wrote.
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

        var test = AnalyzerVerifier.Create(RollupUsageTemplate, AnalyzerVerifier.LegacyEditorConfig("mysql"));
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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig: null);
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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0001")
            .WithArguments("sqlartisan_target_dbms", "postgresql", "PostgreSQL", "sqlartisan_syntax_postgresql"));

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

        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(RollupUsageTemplate), editorConfig);
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

    // Like SQLA0001, SQLA0002 carries no file location, so only a global
    // analyzer config reaches it (docs/analyzer.md's Migrating section names
    // this as the TreatWarningsAsErrors escape hatch) — a file-scoped
    // .editorconfig severity line does not (proven separately: it leaves the
    // legacy pair's own SQLA0100 warning as the only diagnostic here too).
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
}
