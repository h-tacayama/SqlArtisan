using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class NotInSubqueryAnalyzerTests
{
    // Two tables so the subquery reads a different one, as the trap normally
    // appears: the outer column's own nullability is irrelevant here.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public T(string alias = "") : base("t", alias)
            {
                Id = new DbColumn(this, "id");
            }

            [DbColumnMetadata(Nullable = false, HasDefault = false)]
            public DbColumn Id { get; }
        }

        class S : DbTableBase
        {
            public S(string alias = "") : base("s", alias)
            {
                Ref = new DbColumn(this, "ref");
                Key = new DbColumn(this, "key");
                Legacy = new DbColumn(this, "legacy");
            }

            [DbColumnMetadata(Nullable = true, HasDefault = false)]
            public DbColumn Ref { get; }

            [DbColumnMetadata(Nullable = false, HasDefault = false)]
            public DbColumn Key { get; }

            public DbColumn Legacy { get; }
        }

        class C
        {
            void M()
            {
                T t = new T();
                S s = new S();
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements, string column) =>
        RunAsync(
            Usage(statements),
            AnalyzerVerifier.EditorConfig("postgresql"),
            [new DiagnosticResult("SQLA0008", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(column)]);

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
    public Task NotIn_NullableSubqueryColumn_Warns() =>
        RunReporting(
            "var sql = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(Select(s.Ref).From(s))|}).Build();",
            "Ref");

    // The select list is read from the chain's head, however long the chain is.
    [Fact]
    public Task NotIn_NullableColumnBehindFilteredSubquery_Warns() =>
        RunReporting(
            "var sql = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(Select(s.Ref).From(s).Where(s.Key > 0))|}).Build();",
            "Ref");

    // The documented remediation: filtering the NULLs out must silence the rule.
    [Fact]
    public Task NotIn_NullableColumnFilteredByIsNotNull_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Ref).From(s).Where(s.Ref.IsNotNull))).Build();");

    [Fact]
    public Task NotIn_NullableColumnFilteredByIsNotNullAmongOthers_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Ref).From(s).Where(s.Ref.IsNotNull & s.Key > 0))).Build();");

    // IsNotNull on some other column does not clear the selected one. Legacy
    // carries no facts, so the filter itself trips nothing.
    [Fact]
    public Task NotIn_IsNotNullOnDifferentColumn_Warns() =>
        RunReporting(
            "var sql = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(Select(s.Ref).From(s).Where(s.Legacy.IsNotNull))|}).Build();",
            "Ref");

    [Fact]
    public Task NotIn_NotNullSubqueryColumn_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Key).From(s))).Build();");

    [Fact]
    public Task NotIn_SubqueryColumnWithoutMetadata_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Legacy).From(s))).Build();");

    // The values overloads share the name and arity; only the subquery form can
    // swallow a row set this way.
    [Fact]
    public Task NotIn_Values_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.NotIn(1, 2, 3)).Build();");

    // IN has the opposite semantics — a NULL just fails to match — so it is not
    // this rule's business.
    [Fact]
    public Task In_NullableSubqueryColumn_Silent() =>
        RunSilent("var sql = Select(t.Id).From(t).Where(t.Id.In(Select(s.Ref).From(s))).Build();");

    // The remediation arrives held in a variable as readily as written inline,
    // and a predicate the rule cannot read may be the one that filters.
    [Fact]
    public Task NotIn_IsNotNullHeldInLocal_Silent() =>
        RunSilent("""
            SqlCondition filter = s.Ref.IsNotNull;
            var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Ref).From(s).Where(filter))).Build();
            """);

    // Only a condition it cannot read silences the rule; a bound value does not.
    [Fact]
    public Task NotIn_SubqueryFilteredByValueLocal_Warns() =>
        RunReporting(
            """
            int n = 0;
            var sql = Select(t.Id).From(t).Where({|#0:t.Id.NotIn(Select(s.Ref).From(s).Where(s.Ref > n))|}).Build();
            """,
            "Ref");

    [Fact]
    public Task NotIn_SubqueryHeldInVariable_Silent() =>
        RunSilent("""
            ISubquery q = Select(s.Ref).From(s);
            var sql = Select(t.Id).From(t).Where(t.Id.NotIn(q)).Build();
            """);

    [Fact]
    public Task NotIn_NoTargetConfigured_Silent() =>
        RunSilent(
            "var sql = Select(t.Id).From(t).Where(t.Id.NotIn(Select(s.Ref).From(s))).Build();",
            dbms: null);
}
