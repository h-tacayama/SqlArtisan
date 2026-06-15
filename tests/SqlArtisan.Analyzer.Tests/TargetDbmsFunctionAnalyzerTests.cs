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
        var diagnostics = await AnalyzerHarness.RunAsync(Usage("SqlArtisan.Sql.Ceiling(1)"), targetDbms: null);
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
            d.GetMessage().Contains("'Ceiling'") && d.GetMessage().Contains("oracle")
            && d.Location.GetLineSpan().Path == "File0.cs");

        Assert.Contains(diagnostics, d =>
            d.GetMessage().Contains("'Ceil'") && d.GetMessage().Contains("sqlserver")
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
}
