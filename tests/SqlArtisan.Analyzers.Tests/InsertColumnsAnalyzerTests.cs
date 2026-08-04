using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class InsertColumnsAnalyzerTests
{
    // Code is the only column an INSERT must name: NOT NULL and no default.
    // Ident is NOT NULL but engine-assigned, Note is nullable, and Legacy carries
    // no facts at all.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Code = new DbColumn(this, "code");
                Ident = new DbColumn(this, "ident");
                Note = new DbColumn(this, "note");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(Nullable = false, HasDefault = false)]
            public DbColumn Code { get; }

            [DbColumnMetadata(Nullable = false, HasDefault = true)]
            public DbColumn Ident { get; }

            [DbColumnMetadata(Nullable = true, HasDefault = false)]
            public DbColumn Note { get; }

            public DbColumn Legacy { get; }
        }

        class C
        {
            void M()
            {
                T t = new T();
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements, params string[] columns) =>
        RunAsync(
            Usage(statements),
            AnalyzerVerifier.EditorConfig("postgresql"),
            [.. columns.Select(c =>
                new DiagnosticResult("SQLA0009", DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments(c))]);

    private static Task RunSilent(string statements, string? dbms = "postgresql") =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms),
            []);

    private static Task RunAsync(string source, string? editorConfig, DiagnosticResult[] expected)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Fact]
    public Task Insert_OmittingRequiredColumn_Warns() =>
        RunReporting(
            """var sql = {|#0:InsertInto(t, t.Note)|}.Values("x").Build();""",
            "Code");

    [Fact]
    public Task Insert_NamingRequiredColumn_Silent() =>
        RunSilent("""var sql = InsertInto(t, t.Code, t.Note).Values("c", "x").Build();""");

    // The engine supplies it, so omitting it is the normal thing to do.
    [Fact]
    public Task Insert_OmittingDefaultedNotNullColumn_Silent() =>
        RunSilent(
            """var sql = InsertInto(t, t.Code, t.Note, t.Legacy).Values("c", "x", "l").Build();""");

    [Fact]
    public Task Insert_OmittingNullableColumn_Silent() =>
        RunSilent(
            """var sql = InsertInto(t, t.Code, t.Ident, t.Legacy).Values("c", 1, "l").Build();""");

    [Fact]
    public Task Insert_OmittingColumnWithoutMetadata_Silent() =>
        RunSilent(
            """var sql = InsertInto(t, t.Code, t.Ident, t.Note).Values("c", 1, "x").Build();""");

    // The positional form supplies every column by construction.
    [Fact]
    public Task Insert_WithoutColumnList_Silent() =>
        RunSilent("""var sql = InsertInto(t).Values("c", 1, "x", "l").Build();""");

    // INSERT IGNORE asked for error-raising rows to be skipped. Run against MySQL,
    // the only dialect the construct exists on.
    [Fact]
    public Task InsertIgnore_OmittingRequiredColumn_Silent() =>
        RunSilent("""var sql = InsertIgnoreInto(t, t.Note).Values("x").Build();""", dbms: "mysql");

    // A list this rule cannot read in full would make every unread column look
    // omitted, so the statement is skipped instead.
    [Fact]
    public Task Insert_ColumnListBuiltElsewhere_Silent() =>
        RunSilent("""
            DbColumn[] cols = new[] { t.Code, t.Note };
            var sql = InsertInto(t, cols).Values("c", "x").Build();
            """);

    [Fact]
    public Task Insert_InsertSelect_OmittingRequiredColumn_Warns() =>
        RunReporting(
            "var sql = {|#0:InsertInto(t, t.Note)|}.Select(t.Note).From(t).Build();",
            "Code");

    [Fact]
    public Task Insert_NoTargetConfigured_Silent() =>
        RunSilent("""var sql = InsertInto(t, t.Note).Values("x").Build();""", dbms: null);
}
