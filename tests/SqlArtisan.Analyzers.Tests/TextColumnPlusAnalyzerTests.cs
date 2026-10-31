using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class TextColumnPlusAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                First = new DbColumn(this, "first");
                Last = new DbColumn(this, "last");
                Age = new DbColumn(this, "age");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Text)]
            public DbColumn First { get; }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Text)]
            public DbColumn Last { get; }

            [DbColumnMetadata(TypeCategory = DbTypeCategory.Numeric)]
            public DbColumn Age { get; }

            [DbColumnMetadata(Nullable = true)]
            public DbColumn Legacy { get; }
        }

        class C
        {
            void M(string separator)
            {
                T t = new T();
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(
        string statements, string dbms, string column, string dialects) =>
        RunAsync(
            Usage(statements),
            AnalyzerVerifier.EditorConfig(dbms),
            new DiagnosticResult("SQLA0206", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(column, dialects));

    private static Task RunSilent(string statements, string dbms) =>
        RunAsync(AnalyzerVerifier.Unmarked(Usage(statements)), AnalyzerVerifier.EditorConfig(dbms));

    private static async Task RunAsync(
        string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    [Fact]
    public Task TwoTextColumns_MySql_ReportsSqla0206() =>
        RunReporting(
            "var q = Select({|#0:t.First + t.Last|}).From(t);", "mysql", "First", "MySQL");

    [Fact]
    public Task TextColumnBesideStringVariable_Sqlite_ReportsSqla0206() =>
        RunReporting(
            "var q = Select({|#0:t.First + separator|}).From(t);", "sqlite", "First", "SQLite");

    // The column is named whichever side it sits on.
    [Fact]
    public Task TextColumnOnTheRight_Oracle_ReportsSqla0206() =>
        RunReporting(
            "var q = Select({|#0:Coalesce(t.Legacy, t.Age) + t.Last|}).From(t);",
            "oracle",
            "Last",
            "Oracle");

    // A text column holding digits adds as meant on MySQL, but PostgreSQL rejects it.
    [Fact]
    public Task TextColumnBesideNumber_PostgreSql_ReportsSqla0206() =>
        RunReporting(
            "var q = Select({|#0:t.First + 1|}).From(t);", "postgresql", "First", "PostgreSQL");

    [Fact]
    public Task CompoundAssignment_ReportsSqla0206() =>
        RunReporting(
            """
            SqlExpression e = t.Age;
            {|#0:e += t.Last|};
            """,
            "mysql",
            "Last",
            "MySQL");

    [Fact]
    public Task MultipleDialects_JoinsTheFailingOnes() =>
        RunAsync(
            Usage("var q = Select({|#0:t.First + t.Last|}).From(t);"),
            """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_oracle = any
            sqlartisan_syntax_sqlserver = any
            """,
            new DiagnosticResult("SQLA0206", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("First", "MySQL and Oracle"));

    // SQL Server's + concatenates text.
    [Fact]
    public Task TwoTextColumns_SqlServer_StaysSilent() =>
        RunSilent("var q = Select(t.First + t.Last).From(t);", "sqlserver");

    // A string constant beside the column is SQLA0104's, so the + reports once.
    [Fact]
    public Task TextColumnBesideStringConstant_ReportsSqla0104Only() =>
        RunAsync(
            Usage("var q = Select(t.First + {|#0:\" \"|}).From(t);"),
            AnalyzerVerifier.EditorConfig("mysql"),
            new DiagnosticResult("SQLA0104", DiagnosticSeverity.Warning).WithLocation(0));

    // MySQL reads a text column beside an interval as a date.
    [Fact]
    public Task TextColumnBesideInterval_StaysSilent() =>
        RunSilent("var q = Select(t.First + Interval(1, DateTimePart.Day)).From(t);", "mysql");

    [Fact]
    public Task NumericColumns_StaysSilent() =>
        RunSilent("var q = Select(t.Age + t.Age).From(t);", "mysql");

    // Absence of a fact is silence.
    [Fact]
    public Task ColumnsWithoutCategory_StaysSilent() =>
        RunSilent("var q = Select(t.Legacy + t.Legacy).From(t);", "mysql");

    [Fact]
    public Task NoTargetDialectConfigured_StaysSilent() =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage("var q = Select(t.First + t.Last).From(t);")),
            null);
}
