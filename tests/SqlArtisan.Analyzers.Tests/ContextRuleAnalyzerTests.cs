using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class ContextRuleAnalyzerTests
{
    // The marked span is the whole trigger invocation (receiver chain included) —
    // the same location SQLA0002 reports for an instance-chain member.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Dep;
            public T() : base("t", "") { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
        }

        class C
        {
            void M()
            {
                T t = new T();
                T s = new T();
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements) =>
        RunAsync(Usage(statements), AnalyzerVerifier.EditorConfig("mysql"), expectWarning: true);

    private static Task RunSilent(string statements, string? dbms = "mysql") =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms),
            expectWarning: false);

    private static async Task RunAsync(string source, string? editorConfig, bool expectWarning)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        if (expectWarning)
        {
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        }

        await test.RunAsync();
    }

    [Fact]
    public Task LimitInInSubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|}));
            """);

    [Fact]
    public Task LimitInNotInSubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            SqlCondition c = t.Id.NotIn({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInAnySubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            SqlCondition c = t.Id > Any({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInAllSubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            SqlCondition c = t.Id > All({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInSomeSubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            SqlCondition c = t.Id == Some({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitOffsetInInSubquery_MySql_ReportsSqla0004() =>
        RunReporting("""
            SqlCondition c = t.Id.In({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|}.Offset(1));
            """);

    [Fact]
    public Task LimitInInSubquery_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).Where(t.Id.In(Select(s.Id).From(s).OrderBy(s.Id).Limit(2)));
            """, "postgresql");

    [Fact]
    public Task LimitInInSubquery_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).Where(t.Id.In(Select(s.Id).From(s).OrderBy(s.Id).Limit(2)));
            """, dbms: null);

    [Fact]
    public Task LimitTopLevel_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(2);
            """);

    [Fact]
    public Task LimitInExistsSubquery_MySql_StaysSilent() =>
        RunSilent("""
            SqlCondition c = Exists(Select(s.Id).From(s).OrderBy(s.Id).Limit(2));
            """);

    [Fact]
    public Task LimitInScalarSubquery_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(Select(s.Id).From(s).OrderBy(s.Id).Limit(1).As("x")).From(t);
            """);

    [Fact]
    public Task LimitInCteBody_MySql_StaysSilent() =>
        RunSilent("""
            var cte = new Cte("c");
            var body = cte.As(Select(s.Id).From(s).OrderBy(s.Id).Limit(2));
            """);

    [Fact]
    public Task LimitInDerivedTableInsideInSubquery_MySql_StaysSilent() =>
        RunSilent("""
            SqlCondition c = t.Id.In(Select(Bind(1)).From(Select(s.Id).From(s).OrderBy(s.Id).Limit(2).AsTable("d")));
            """);

    [Fact]
    public Task LimitViaVariable_MySql_StaysSilent() =>
        RunSilent("""
            var sub = Select(s.Id).From(s).OrderBy(s.Id).Limit(2);
            SqlCondition c = t.Id.In(sub);
            """);

    [Fact]
    public Task LimitViaHelperMethod_MySql_StaysSilent() =>
        RunAsync("""
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
                    SqlCondition c = t.Id.In(Sub(new T()));
                }

                static ISubquery Sub(T s) => Select(s.Id).From(s).OrderBy(s.Id).Limit(2);
            }
            """, AnalyzerVerifier.EditorConfig("mysql"), expectWarning: false);

    [Fact]
    public Task GroupingWithoutWithRollup_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select(t.Dep, {|#0:Grouping(t.Dep)|}).From(t).GroupBy(t.Dep).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingMultiArgWithoutWithRollup_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select({|#0:Grouping(t.Dep, t.Id)|}).From(t).GroupBy(t.Dep, t.Id).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingInHavingWithoutWithRollup_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select(t.Dep).From(t).GroupBy(t.Dep).Having({|#0:Grouping(t.Dep)|} == 0);
            """);

    [Fact]
    public Task GroupingAliasedWithoutWithRollup_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select({|#0:Grouping(t.Dep)|}.As("g"), t.Dep).From(t).GroupBy(t.Dep).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingSplitChainWithGroupByVisible_MySql_ReportsSqla0004() =>
        RunReporting("""
            var q = Select(t.Dep).From(t);
            var r = q.GroupBy(t.Dep).Having({|#0:Grouping(t.Dep)|} == 0);
            """);

    [Fact]
    public Task GroupingWithWithRollup_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep, Grouping(t.Dep)).From(t).GroupBy(t.Dep).WithRollup();
            """);

    [Fact]
    public Task GroupingInOrderByWithWithRollup_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep).From(t).GroupBy(t.Dep).WithRollup().OrderBy(Grouping(t.Dep));
            """);

    [Fact]
    public Task GroupingChainEndsAtGroupBy_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep, Grouping(t.Dep)).From(t).GroupBy(t.Dep);
            """);

    [Fact]
    public Task GroupingNoGroupByVisible_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep, Grouping(t.Dep)).From(t);
            var r = q.GroupBy(t.Dep).WithRollup();
            """);

    [Fact]
    public Task GroupingWithoutWithRollup_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep, Grouping(t.Dep)).From(t).GroupBy(t.Dep).OrderBy(t.Dep);
            """, "postgresql");

    [Fact]
    public Task GroupingInInnerSubqueryWithOwnWithRollup_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).Where(t.Id.In(Select(Grouping(s.Dep)).From(s).GroupBy(s.Dep).WithRollup()));
            """);

    // Regression for a cross-query misattribution: climbing through the WHERE/IN
    // boundary and the outer AND must never reach the unrelated outer Having.
    [Fact]
    public Task GroupingInWhereOfDifferentQuery_MySql_StaysSilent() =>
        RunSilent("""
            var outer = Select(t.Dep).From(t).GroupBy(t.Dep).Having(
                t.Id.In(Select(s.Dep).From(s).Where(Grouping(s.Dep) == 0).GroupBy(s.Dep).WithRollup().Having(s.Dep > 0))
                & (t.Dep > 0));
            """);
}
