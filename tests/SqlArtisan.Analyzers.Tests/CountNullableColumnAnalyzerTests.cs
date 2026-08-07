using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class CountNullableColumnAnalyzerTests
{
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Id = new DbColumn(this, "id");
                Note = new DbColumn(this, "note");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(Nullable = false, HasDefault = false)]
            public DbColumn Id { get; }

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

    // The rule ships off; every reporting test has to ask for it, which is the
    // opt-in this severity line documents.
    private static string EditorConfig(string severity) => $"""
        root = true

        [*.cs]
        sqlartisan_syntax_postgresql = any
        dotnet_diagnostic.SQLA0203.severity = {severity}
        """;

    private static Task RunReporting(string statements, string column) =>
        RunAsync(
            Usage(statements),
            EditorConfig("suggestion"),
            [new DiagnosticResult("SQLA0203", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments(column)]);

    private static Task RunSilent(string statements, string? editorConfig = null) =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            editorConfig ?? EditorConfig("suggestion"),
            []);

    private static Task RunAsync(string source, string? editorConfig, DiagnosticResult[] expected)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Fact]
    public Task Count_NullableColumn_Warns() =>
        RunReporting(
            "var sql = Select({|#0:Count(t.Note)|}).From(t).Build();",
            "Note");

    [Fact]
    public Task Count_NullableColumnInHaving_Warns() =>
        RunReporting(
            "var sql = Select(t.Id).From(t).GroupBy(t.Id).Having({|#0:Count(t.Note)|} > 0).Build();",
            "Note");

    // An expression wrapping the count does not hide the query it sits in.
    [Fact]
    public Task Count_WrappedInCoalesce_Warns() =>
        RunReporting("var sql = Select(Coalesce({|#0:Count(t.Note)|}, 0)).From(t).Build();", "Note");

    // The rule reports correct code, so it must stay off until asked for. Asserted
    // on the descriptor because the test harness force-enables every supported
    // diagnostic, and so cannot observe the default.
    [Fact]
    public void CountNullableColumn_IsOptIn()
    {
        DiagnosticDescriptor descriptor = Assert.Single(
            new DialectUsageAnalyzer().SupportedDiagnostics,
            d => d.Id == "SQLA0203");

        Assert.False(descriptor.IsEnabledByDefault);
        Assert.Equal(DiagnosticSeverity.Info, descriptor.DefaultSeverity);
    }

    // Past an outer join, counting the column is how you count matched rows —
    // COUNT(*) would count the unmatched ones too, so the advice would be wrong.
    [Fact]
    public Task Count_NullableColumnAfterLeftJoin_Silent() =>
        RunSilent("""
            T r = new T("r");
            var sql = Select(t.Id, Count(r.Note)).From(t).LeftJoin(r).On(t.Id == r.Id).GroupBy(t.Id).Build();
            """);

    // Built apart from the query, the count carries no join context to read.
    [Fact]
    public Task Count_HeldInLocal_Silent() =>
        RunSilent("""
            T r = new T("r");
            var counted = Count(r.Note);
            var sql = Select(t.Id, counted).From(t).LeftJoin(r).On(t.Id == r.Id).GroupBy(t.Id).Build();
            """);

    [Fact]
    public Task Count_NotNullColumn_Silent() =>
        RunSilent("var sql = Select(Count(t.Id)).From(t).Build();");

    [Fact]
    public Task Count_ColumnWithoutMetadata_Silent() =>
        RunSilent("var sql = Select(Count(t.Legacy)).From(t).Build();");

    // COUNT(*) counts rows already, and the overload it resolves to is the one
    // this rule must not read a column out of.
    [Fact]
    public Task Count_Asterisk_Silent() =>
        RunSilent("var sql = Select(Count(Asterisk)).From(t).Build();");

    // COUNT(DISTINCT col) asks for distinct values, so COUNT(*) is no substitute.
    [Fact]
    public Task Count_DistinctNullableColumn_Silent() =>
        RunSilent("var sql = Select(Count(Distinct, t.Note)).From(t).Build();");

    [Fact]
    public Task Count_NonColumnExpression_Silent() =>
        RunSilent("var sql = Select(Count(Upper(t.Note))).From(t).Build();");

    [Fact]
    public Task Count_NoTargetConfigured_Silent() =>
        RunSilent(
            "var sql = Select(Count(t.Note)).From(t).Build();",
            "root = true\n\n[*.cs]\ndotnet_diagnostic.SQLA0203.severity = suggestion");
}
