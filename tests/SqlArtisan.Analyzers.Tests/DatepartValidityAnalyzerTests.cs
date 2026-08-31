using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class DatepartValidityAnalyzerTests
{
    private static string Usage(string statement) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public DbColumn CreatedAt;
            public T() : base("t", "") { CreatedAt = new DbColumn(this, "created_at"); }
        }

        class C
        {
            void M()
            {
                T t = new T();
                {{statement}}
            }
        }
        """;

    private static async Task RunAsync(string statement, string editorConfig, bool expectWarning)
    {
        var test = AnalyzerVerifier.Create(
            expectWarning ? Usage(statement) : AnalyzerVerifier.Unmarked(Usage(statement)),
            editorConfig);

        if (expectWarning)
        {
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0104").WithLocation(0));
        }

        await test.RunAsync();
    }

    // --- Extract x MySQL (MySqlTemporalUnits) ---

    [Fact]
    public Task Extract_MySql_ValidUnit_StaysSilent() =>
        RunAsync(
            "var s = Select(Extract(DateTimePart.Day, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: false);

    [Fact]
    public Task Extract_MySql_InvalidUnit_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Extract({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: true);

    // --- Extract x Oracle (OracleExtractFields) ---

    [Fact]
    public Task Extract_Oracle_ValidField_StaysSilent() =>
        RunAsync(
            "var s = Select(Extract(DateTimePart.Year, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("oracle"),
            expectWarning: false);

    [Fact]
    public Task Extract_Oracle_InvalidField_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Extract({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("oracle"),
            expectWarning: true);

    // --- Extract x PostgreSQL (PostgreSqlExtractFields) ---

    [Fact]
    public Task Extract_PostgreSql_ValidField_StaysSilent() =>
        RunAsync(
            "var s = Select(Extract(DateTimePart.Epoch, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("postgresql"),
            expectWarning: false);

    [Fact]
    public Task Extract_PostgreSql_InvalidField_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Extract({|#0:DateTimePart.Weekday|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("postgresql"),
            expectWarning: true);

    // --- Datepart x SQL Server (SqlServerDatepartFields) ---

    [Fact]
    public Task Datepart_SqlServer_ValidDatepart_StaysSilent() =>
        RunAsync(
            "var s = Select(Datepart(DateTimePart.Year, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: false);

    [Fact]
    public Task Datepart_SqlServer_InvalidDatepart_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Datepart({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: true);

    // --- Dateadd x SQL Server (SqlServerDatepartFields) ---

    [Fact]
    public Task Dateadd_SqlServer_ValidDatepart_StaysSilent() =>
        RunAsync(
            "var s = Select(Dateadd(DateTimePart.Day, 1, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: false);

    [Fact]
    public Task Dateadd_SqlServer_InvalidDatepart_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Dateadd({|#0:DateTimePart.Epoch|}, 1, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: true);

    // --- Datediff x SQL Server (SqlServerDatepartFields) ---

    [Fact]
    public Task Datediff_SqlServer_ValidDatepart_StaysSilent() =>
        RunAsync(
            "var s = Select(Datediff(DateTimePart.Day, t.CreatedAt, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: false);

    [Fact]
    public Task Datediff_SqlServer_InvalidDatepart_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Datediff({|#0:DateTimePart.Epoch|}, t.CreatedAt, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver"),
            expectWarning: true);

    // --- Timestampadd x MySQL (MySqlTimestampUnits) ---

    [Fact]
    public Task Timestampadd_MySql_ValidUnit_StaysSilent() =>
        RunAsync(
            "var s = Select(Timestampadd(DateTimePart.Day, 1, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: false);

    [Fact]
    public Task Timestampadd_MySql_InvalidUnit_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Timestampadd({|#0:DateTimePart.DayHour|}, 1, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: true);

    // --- Timestampdiff x MySQL (MySqlTimestampUnits) ---

    [Fact]
    public Task Timestampdiff_MySql_ValidUnit_StaysSilent() =>
        RunAsync(
            "var s = Select(Timestampdiff(DateTimePart.Day, t.CreatedAt, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: false);

    [Fact]
    public Task Timestampdiff_MySql_InvalidUnit_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Timestampdiff({|#0:DateTimePart.DayHour|}, t.CreatedAt, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: true);

    // --- DateTrunc x PostgreSQL (PostgreSqlDateTruncFields) ---

    [Fact]
    public Task DateTrunc_PostgreSql_ValidField_StaysSilent() =>
        RunAsync(
            "var s = Select(DateTrunc(DateTimePart.Month, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("postgresql"),
            expectWarning: false);

    [Fact]
    public Task DateTrunc_PostgreSql_InvalidField_ReportsSqla0104() =>
        RunAsync(
            // Epoch is EXTRACT-only on PostgreSQL — date_trunc rejects it.
            "var s = Select(DateTrunc({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("postgresql"),
            expectWarning: true);

    // --- Datetrunc x SQL Server (SqlServerDateTruncFields) ---

    [Fact]
    public Task Datetrunc_SqlServer_ValidDatepart_StaysSilent() =>
        RunAsync(
            "var s = Select(Datetrunc(DateTimePart.Day, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver", "2022"),
            expectWarning: false);

    [Fact]
    public Task Datetrunc_SqlServer_InvalidDatepart_ReportsSqla0104() =>
        RunAsync(
            // Weekday is DATEPART-valid but DATETRUNC excludes it (learn.microsoft.com).
            "var s = Select(Datetrunc({|#0:DateTimePart.Weekday|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("sqlserver", "2022"),
            expectWarning: true);

    // --- Interval x MySQL (MySqlTemporalUnits) ---

    [Fact]
    public Task Interval_MySql_ValidUnit_StaysSilent() =>
        RunAsync(
            "var s = Select(t.CreatedAt + Interval(1, DateTimePart.Day)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: false);

    [Fact]
    public Task Interval_MySql_InvalidUnit_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(t.CreatedAt + Interval(1, {|#0:DateTimePart.Epoch|})).From(t).Build();",
            AnalyzerVerifier.EditorConfig("mysql"),
            expectWarning: true);

    // --- Cross-cutting behavior ---

    // A cast or implicit constant is still a compile-time constant the rule can
    // resolve, so it must not fall through the non-constant escape.
    [Fact]
    public Task Extract_Oracle_CastConstantInvalidField_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Extract({|#0:(DateTimePart)10|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("oracle"),
            expectWarning: true);

    // This pair runs the same `Extract(0, ...)` source against two dialects: 0
    // resolves to DateTimePart.Century, which PostgreSQL's EXTRACT has and
    // Oracle's does not, so the resolved member decides — not the literal.
    [Fact]
    public Task Extract_PostgreSql_ImplicitZeroConstantValidField_StaysSilent() =>
        RunAsync(
            "var s = Select(Extract(0, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("postgresql"),
            expectWarning: false);

    [Fact]
    public Task Extract_Oracle_ImplicitZeroConstantInvalidField_ReportsSqla0104() =>
        RunAsync(
            "var s = Select(Extract({|#0:0|}, t.CreatedAt)).From(t).Build();",
            AnalyzerVerifier.EditorConfig("oracle"),
            expectWarning: true);

    [Fact]
    public Task NonConstantDatepart_StaysSilent()
    {
        string source = AnalyzerVerifier.Unmarked(Usage("""
            void N(DateTimePart part)
            {
                var s = Select(Extract(part, t.CreatedAt)).From(t).Build();
            }
            """));
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("mysql"));
        return test.RunAsync();
    }

    [Fact]
    public Task NoTargetConfigured_StaysSilent()
    {
        string source = AnalyzerVerifier.Unmarked(Usage(
            "var s = Select(Extract(DateTimePart.Epoch, t.CreatedAt)).From(t).Build();"));
        var test = AnalyzerVerifier.Create(source);
        return test.RunAsync();
    }

    [Fact]
    public async Task InvalidOnTwoDialects_JoinsIntoOneDiagnostic()
    {
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_oracle = any
            """;

        var test = AnalyzerVerifier.Create(
            Usage("var s = Select(Extract({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();"),
            editorConfig);
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0104")
                .WithLocation(0)
                .WithMessage("'Epoch' is not a valid datepart for 'Extract' on MySQL and Oracle"));

        await test.RunAsync();
    }

    [Fact]
    public async Task VersionBoundDialect_SkipsToAvoidDoubleReportingWithSqla0101()
    {
        // Datetrunc's matrix entry requires SQL Server 2022+; declaring 2016 makes
        // SQLA0101 (version-bound) the construct-level verdict for this usage, so
        // SQLA0104 must not also fire even though Weekday is not a Datetrunc value.
        var test = AnalyzerVerifier.Create(
            Usage("var s = Select({|#0:Datetrunc(DateTimePart.Weekday, t.CreatedAt)|}).From(t).Build();"),
            AnalyzerVerifier.EditorConfig("sqlserver", "2016"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0101").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task UnsupportedOverride_SkipsToAvoidDoubleReportingWithSqla0100()
    {
        // Forcing the construct `unsupported` makes SQLA0100 the construct-level
        // verdict on every target — the never-both-fire contract covers the
        // override path, not just the matrix's own verdict (release audit pass 1).
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = any
            sqlartisan_construct_extract = unsupported
            """;

        var test = AnalyzerVerifier.Create(
            Usage("var s = Select({|#0:Extract(DateTimePart.Epoch, t.CreatedAt)|}).From(t).Build();"),
            editorConfig);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task SupportedOverride_BelowVersionBound_StillReportsInvalidDatepart()
    {
        // A `supported` override silences SQLA0100/0101 on the asserted dialect;
        // the datepart check must then re-arm there, or the invalid argument
        // goes wholly undiagnosed (release audit pass 2).
        const string editorConfig = """
            root = true

            [*.cs]
            sqlartisan_syntax_sqlserver = 2016
            sqlartisan_construct_datetrunc = supported
            """;

        var test = AnalyzerVerifier.Create(
            Usage("var s = Select(Datetrunc({|#0:DateTimePart.Weekday|}, t.CreatedAt)).From(t).Build();"),
            editorConfig);
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0104")
                .WithLocation(0)
                .WithMessage("'Weekday' is not a valid datepart for 'Datetrunc' on SQL Server"));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExactMessage_NamesTheMemberDatepartAndDialect()
    {
        var test = AnalyzerVerifier.Create(
            Usage("var s = Select(Extract({|#0:DateTimePart.Epoch|}, t.CreatedAt)).From(t).Build();"),
            AnalyzerVerifier.EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0104")
                .WithLocation(0)
                .WithMessage("'Epoch' is not a valid datepart for 'Extract' on Oracle"));

        await test.RunAsync();
    }
}
