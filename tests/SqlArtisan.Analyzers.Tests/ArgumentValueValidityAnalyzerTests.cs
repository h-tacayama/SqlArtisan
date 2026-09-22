using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class ArgumentValueValidityAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Name;
            public T() : base("t", "") { Id =
                new DbColumn(this, "id"); Name = new DbColumn(this, "name"); }
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

    private static Task RunReporting(
        string statements, string editorConfig, params string[] messageArguments) =>
        RunAsync(Usage(statements), editorConfig, "SQLA0105", messageArguments);

    private static Task RunSilent(string statements, string editorConfig) =>
        RunAsync(AnalyzerVerifier.Unmarked(Usage(statements)), editorConfig, null);

    // The dialect a construct-level verdict owns still reports it, so the
    // silence under test is SQLA0105's alone: SQLA0100 is expected by id.
    private static Task RunOwnedBySqla0100(string statements, string editorConfig) =>
        RunAsync(Usage(statements), editorConfig, "SQLA0100");

    private static async Task RunAsync(
        string source,
        string editorConfig,
        string? expectedId,
        params string[] messageArguments)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);

        if (expectedId is not null)
        {
            DiagnosticResult expected =
                DiagnosticResult.CompilerWarning(expectedId).WithLocation(0);
            test.ExpectedDiagnostics.Add(
                messageArguments.Length == 0
                    ? expected
                    : expected.WithArguments(messageArguments));
        }

        await test.RunAsync();
    }

    // --- RegexpOptions x the match-parameter alphabet (#528) ---

    [Fact]
    public Task RegexpLike_MySql_ExcludingWhiteSpace_ReportsSqla0105() =>
        RunReporting(
            """
            SqlCondition c = RegexpLike(t.Name, "ab",
                {|#0:RegexpOptions.ExcludingWhiteSpace|});
            """,
            AnalyzerVerifier.EditorConfig("mysql"),
            "RegexpLike", "ExcludingWhiteSpace", "match option", "MySQL");

    [Fact]
    public Task RegexpLike_MySql_CaseInsensitive_StaysSilent() =>
        RunSilent(
            """SqlCondition c = RegexpLike(t.Name, "ab", RegexpOptions.CaseInsensitive);""",
            AnalyzerVerifier.EditorConfig("mysql"));

    [Fact]
    public Task RegexpLike_Oracle_ExcludingWhiteSpace_StaysSilent() =>
        RunSilent(
            """
            SqlCondition c = RegexpLike(t.Name, "ab", RegexpOptions.ExcludingWhiteSpace);
            """,
            AnalyzerVerifier.EditorConfig("oracle"));

    [Fact]
    public Task RegexpLike_PostgreSql_ExcludingWhiteSpace_StaysSilent() =>
        RunSilent(
            """
            SqlCondition c = RegexpLike(t.Name, "ab", RegexpOptions.ExcludingWhiteSpace);
            """,
            AnalyzerVerifier.EditorConfig("postgresql"));

    // The enum is [Flags], so the rule reads a combination: the letter MySQL
    // has is silent and the letter it lacks still reports, from one argument.
    [Fact]
    public Task RegexpLike_MySql_CombinationCarryingTheGap_ReportsSqla0105Once() =>
        RunReporting(
            """
            SqlCondition c = RegexpLike(t.Name, "ab",
                {|#0:RegexpOptions.CaseInsensitive | RegexpOptions.ExcludingWhiteSpace|});
            """,
            AnalyzerVerifier.EditorConfig("mysql"));

    [Fact]
    public Task RegexpLike_MySql_None_StaysSilent() =>
        RunSilent(
            """SqlCondition c = RegexpLike(t.Name, "ab", RegexpOptions.None);""",
            AnalyzerVerifier.EditorConfig("mysql"));

    [Fact]
    public Task RegexpReplace_MySql_ExcludingWhiteSpace_ReportsSqla0105() =>
        RunReporting(
            """
            var e = RegexpReplace(t.Name, "ab", "x", 1, 0,
                {|#0:RegexpOptions.ExcludingWhiteSpace|});
            """,
            AnalyzerVerifier.EditorConfig("mysql"));

    // REGEXP_COUNT does not exist on MySQL at all, so the construct-level
    // verdict owns the usage and this rule adds nothing.
    [Fact]
    public Task RegexpCount_MySql_ExcludingWhiteSpace_IsOwnedBySqla0100() =>
        RunOwnedBySqla0100(
            """
            var e = {|#0:RegexpCount(t.Name, "ab", 1, RegexpOptions.ExcludingWhiteSpace)|};
            """,
            AnalyzerVerifier.EditorConfig("mysql"));

    [Fact]
    public Task RegexpLike_NonConstantOptions_StaysSilent() =>
        RunSilent(
            """
            RegexpOptions o = System.DateTime.Now.Ticks > 0
                ? RegexpOptions.ExcludingWhiteSpace : RegexpOptions.None;
            SqlCondition c = RegexpLike(t.Name, "ab", o);
            """,
            AnalyzerVerifier.EditorConfig("mysql"));

    // --- Negative row counts (#529) ---

    [Fact]
    public Task Limit_MySql_NegativeCount_ReportsSqla0105() =>
        RunReporting(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit({|#0:-1|});""",
            AnalyzerVerifier.EditorConfig("mysql"),
            "Limit", "-1", "row count", "MySQL");

    [Fact]
    public Task Limit_PostgreSql_NegativeCount_ReportsSqla0105() =>
        RunReporting(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit({|#0:-1|});""",
            AnalyzerVerifier.EditorConfig("postgresql"));

    // SQLite reads LIMIT -1 as "no limit", so the value is meaningful there.
    [Fact]
    public Task Limit_Sqlite_NegativeCount_StaysSilent() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(-1);""",
            AnalyzerVerifier.EditorConfig("sqlite"));

    [Fact]
    public Task Limit_MySql_ZeroCount_StaysSilent() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(0);""",
            AnalyzerVerifier.EditorConfig("mysql"));

    [Fact]
    public Task Top_SqlServer_NegativeCount_ReportsSqla0105() =>
        RunReporting(
            """var q = Select(Top({|#0:-1|}), t.Id).From(t);""",
            AnalyzerVerifier.EditorConfig("sqlserver"));

    [Fact]
    public Task FetchFirst_PostgreSql_NegativeCount_ReportsSqla0105() =>
        RunReporting(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).FetchFirst({|#0:-1|});""",
            AnalyzerVerifier.EditorConfig("postgresql"));

    // Oracle executes a negative FETCH count, so both spellings stay silent.
    [Fact]
    public Task FetchFirst_Oracle_NegativeCount_StaysSilent() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).FetchFirst(-1);""",
            AnalyzerVerifier.EditorConfig("oracle"));

    [Fact]
    public Task FetchNext_Oracle_NegativeCount_StaysSilent() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).OffsetRows(0).FetchNext(-1);""",
            AnalyzerVerifier.EditorConfig("oracle"));

    [Fact]
    public Task FetchNext_SqlServer_NegativeCount_ReportsSqla0105() =>
        RunReporting(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).OffsetRows(0).FetchNext({|#0:-1|});""",
            AnalyzerVerifier.EditorConfig("sqlserver"));

    [Fact]
    public Task Limit_NonConstantCount_StaysSilent() =>
        RunSilent(
            """
            int n = System.DateTime.Now.Second - 1;
            var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(n);
            """,
            AnalyzerVerifier.EditorConfig("mysql"));

    // --- The shared never-both-fire contract ---

    [Fact]
    public Task Limit_UnsupportedOverride_IsOwnedBySqla0100() =>
        RunOwnedBySqla0100(
            """
            var q = {|#0:Select(t.Id).From(t).OrderBy(t.Id).Limit(-1)|};
            """,
            """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_construct_limit = unsupported
            """);

    // A `supported` override asserts the construct runs, not that every value
    // does, so it re-arms this check on a dialect the matrix flags unsupported.
    [Fact]
    public Task Limit_SupportedOverride_OnOracle_ReportsNothingWithoutAFact() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(-1);""",
            """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = any
            sqlartisan_construct_limit = supported
            """);

    [Fact]
    public Task Limit_MultipleDialects_JoinsTheFailingOnes() =>
        RunReporting(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit({|#0:-1|});""",
            """
            root = true

            [*.cs]
            sqlartisan_syntax_mysql = any
            sqlartisan_syntax_postgresql = any
            sqlartisan_syntax_sqlite = any
            """,
            "Limit", "-1", "row count", "MySQL and PostgreSQL");

    [Fact]
    public Task Limit_NoTargetConfigured_StaysSilent() =>
        RunSilent(
            """var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(-1);""",
            """
            root = true

            [*.cs]
            """);
}
