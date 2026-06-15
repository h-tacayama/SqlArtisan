namespace SqlArtisan.Analyzer.Tests;

// Proves the opt-in dialect analyzer over the permissive single Sql API:
// it flags an unsupported function for the configured DBMS, stays silent
// otherwise, and is fully off until a target DBMS is set.
public class TargetDbmsFunctionAnalyzerTests
{
    private const string Stub = @"
namespace SqlArtisan
{
    public static class Sql
    {
        public static object Abs(object x) => x;
        public static object Ceil(object x) => x;
        public static object Ceiling(object x) => x;
        public static object Round(object x) => x;
        public static object Round(object x, object n) => x;
    }
}
";

    private static string Usage(string call) =>
        Stub + "namespace App { class C { void M() { var _ = " + call + "; } } }";

    [Fact]
    public async Task Flags_Ceiling_When_Target_Is_Oracle()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceiling(1)"), "Oracle");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0001", diagnostic.Id);
        Assert.Contains("Ceiling", diagnostic.GetMessage());
        Assert.Contains("Oracle", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Does_Not_Flag_Ceil_When_Target_Is_Oracle()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceil(1)"), "Oracle");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_Not_Flag_Universal_Abs()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Abs(1)"), "Oracle");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Is_Silent_When_No_Target_Configured()
    {
        // Opt-in: with no <SqlArtisanTargetDbms>, the analyzer does nothing.
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceiling(1)"), msbuildTarget: null);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_Not_Flag_Ceiling_When_Target_Is_SqlServer()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceiling(1)"), "SqlServer");
        Assert.Empty(diagnostics);
    }

    // Per-file .editorconfig scoping: two files in ONE compilation pin different
    // DBMS, and each is checked against its own target — the Oracle file flags
    // Ceiling, the SqlServer file flags Ceil, in the same build.
    [Fact]
    public async Task PerFile_EditorConfig_Scopes_Each_File_To_Its_Own_Dbms()
    {
        string oracleFile = "namespace A1 { class C { void M() { var _ = SqlArtisan.Sql.Ceiling(1); } } }";
        string sqlServerFile = "namespace A2 { class C { void M() { var _ = SqlArtisan.Sql.Ceil(1); } } }";

        var diagnostics = await AnalyzerHarness.RunPerFileAsync(
            Stub,
            (oracleFile, "oracle"),        // Oracle has no CEILING -> flagged here
            (sqlServerFile, "sqlserver")); // SQL Server has no CEIL -> flagged here

        Assert.Equal(2, diagnostics.Length);

        Assert.Contains(diagnostics, d =>
            d.GetMessage().Contains("'Ceiling'") && d.GetMessage().Contains("Oracle")
            && d.Location.GetLineSpan().Path == "File0.cs");

        Assert.Contains(diagnostics, d =>
            d.GetMessage().Contains("'Ceil'") && d.GetMessage().Contains("SqlServer")
            && d.Location.GetLineSpan().Path == "File1.cs");
    }

    // The flip side: each file's *supported* spelling is NOT flagged, proving the
    // scoping is real (not a global pass/fail).
    [Fact]
    public async Task PerFile_Supported_Spelling_Is_Not_Flagged()
    {
        string oracleFile = "namespace A1 { class C { void M() { var _ = SqlArtisan.Sql.Ceil(1); } } }";
        string sqlServerFile = "namespace A2 { class C { void M() { var _ = SqlArtisan.Sql.Ceiling(1); } } }";

        var diagnostics = await AnalyzerHarness.RunPerFileAsync(
            Stub,
            (oracleFile, "oracle"),        // Oracle supports CEIL
            (sqlServerFile, "sqlserver")); // SQL Server supports CEILING

        Assert.Empty(diagnostics);
    }

    // ── Clause-level verbs (UPSERT / MERGE), not just Sql-facade functions ────

    // A fluent-builder shape mimicking the real API: the verbs live on builder
    // types (here under SqlArtisan), reached via a chain — exactly what the
    // analyzer must now recognise beyond the Sql facade.
    private const string BuilderStub = @"
namespace SqlArtisan
{
    public static class Sql
    {
        public static IInsert InsertInto() => null!;
        public static IMerge MergeInto() => null!;
    }
    public interface IInsert { IConflict OnConflict(); object OnDuplicateKeyUpdate(); }
    public interface IConflict { object DoUpdateSet(); }
    public interface IMerge { object Using(); }
}
";

    private static string BuilderUsage(string call) =>
        BuilderStub + "namespace App { class C { void M() { var _ = " + call + "; } } }";

    [Fact]
    public async Task Flags_MergeInto_When_Target_Has_No_Merge()
    {
        // MERGE is Oracle/SqlServer only; PostgreSQL must be flagged.
        var diagnostics = await AnalyzerHarness.RunAsync(BuilderUsage("SqlArtisan.Sql.MergeInto()"), "PostgreSql");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0001", diagnostic.Id);
        Assert.Contains("MergeInto", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Does_Not_Flag_MergeInto_On_Oracle()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(BuilderUsage("SqlArtisan.Sql.MergeInto()"), "Oracle");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Flags_OnDuplicateKeyUpdate_On_PostgreSql()
    {
        // ON DUPLICATE KEY UPDATE is MySQL-only — the verb-mismatch hole that
        // option ② leaves silent at runtime is caught here at build time.
        var diagnostics = await AnalyzerHarness.RunAsync(
            BuilderUsage("SqlArtisan.Sql.InsertInto().OnDuplicateKeyUpdate()"), "PostgreSql");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("OnDuplicateKeyUpdate", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Flags_OnConflict_On_MySql()
    {
        // ON CONFLICT is PostgreSQL/SQLite only.
        var diagnostics = await AnalyzerHarness.RunAsync(
            BuilderUsage("SqlArtisan.Sql.InsertInto().OnConflict()"), "MySql");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("OnConflict", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Does_Not_Flag_OnConflict_On_PostgreSql()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            BuilderUsage("SqlArtisan.Sql.InsertInto().OnConflict()"), "PostgreSql");
        Assert.Empty(diagnostics);
    }

    // ── Arity (SQLA0003): SQL Server's ROUND requires the length argument ─────

    [Fact]
    public async Task Flags_SingleArg_Round_On_SqlServer()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Round(1)"), "SqlServer");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0003", diagnostic.Id);
        Assert.Contains("Round", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Does_Not_Flag_TwoArg_Round_On_SqlServer()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Round(1, 2)"), "SqlServer");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Does_Not_Flag_SingleArg_Round_On_Oracle()
    {
        // Oracle allows ROUND(x); the arity rule is SQL-Server-specific.
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Round(1)"), "Oracle");
        Assert.Empty(diagnostics);
    }

    // ── Unknown target (SQLA0002) + aliases ──────────────────────────────────

    [Fact]
    public async Task Flags_Unknown_Target_Value()
    {
        // A typo'd target must not silently flag everything — it's its own error.
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Abs(1)"), "Oracel");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0002", diagnostic.Id);
        Assert.Contains("Oracel", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Accepts_Alias_Mssql_As_SqlServer()
    {
        // 'mssql' resolves to SqlServer, which has no CEIL -> SQLA0001 (proves the alias).
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceil(1)"), "mssql");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0001", diagnostic.Id);
    }

    [Fact]
    public async Task Target_Is_Case_Insensitive()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceiling(1)"), "ORACLE");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0001", diagnostic.Id);
    }

    // ── Config precedence: .editorconfig overrides the MSBuild property ───────

    [Fact]
    public async Task EditorConfig_Overrides_MsBuild_Property()
    {
        // MSBuild says SqlServer (Ceil invalid), .editorconfig says Oracle (Ceil ok).
        // If .editorconfig wins, there is no diagnostic.
        var diagnostics = await AnalyzerHarness.RunAsync(
            Usage("SqlArtisan.Sql.Ceil(1)"),
            msbuildTarget: "SqlServer",
            editorConfigTarget: "Oracle");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MsBuild_Property_Used_As_Fallback_When_No_EditorConfig()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            Usage("SqlArtisan.Sql.Ceil(1)"),
            msbuildTarget: "SqlServer");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SQLA0001", diagnostic.Id);   // Ceil not on SqlServer
    }
}
