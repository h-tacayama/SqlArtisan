using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class ContextRuleAnalyzerTests
{
    // The marked span is the whole trigger invocation (receiver chain included) —
    // the same location SQLA0100 reports for an instance-chain member.
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

    private static Task RunReporting(string statements, string dbms = "mysql") =>
        RunAsync(Usage(statements), AnalyzerVerifier.EditorConfig(dbms), expectWarning: true);

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
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(0));
        }

        await test.RunAsync();
    }

    [Fact]
    public Task LimitInInSubquery_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|}));
            """);

    [Fact]
    public Task LimitInNotInSubquery_MySql_ReportsSqla0102() =>
        RunReporting("""
            SqlCondition c = t.Id.NotIn({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInAnySubquery_MySql_ReportsSqla0102() =>
        RunReporting("""
            SqlCondition c = t.Id > Any({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInAllSubquery_MySql_ReportsSqla0102() =>
        RunReporting("""
            SqlCondition c = t.Id > All({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitInSomeSubquery_MySql_ReportsSqla0102() =>
        RunReporting("""
            SqlCondition c = t.Id == Some({|#0:Select(s.Id).From(s).OrderBy(s.Id).Limit(2)|});
            """);

    [Fact]
    public Task LimitOffsetInInSubquery_MySql_ReportsSqla0102() =>
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
    public Task GroupingWithoutWithRollup_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(t.Dep, {|#0:Grouping(t.Dep)|}).From(t).GroupBy(t.Dep).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingMultiArgWithoutWithRollup_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Grouping(t.Dep, t.Id)|}).From(t).GroupBy(t.Dep, t.Id).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingInHavingWithoutWithRollup_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(t.Dep).From(t).GroupBy(t.Dep).Having({|#0:Grouping(t.Dep)|} == 0);
            """);

    [Fact]
    public Task GroupingAliasedWithoutWithRollup_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Grouping(t.Dep)|}.As("g"), t.Dep).From(t).GroupBy(t.Dep).OrderBy(t.Dep);
            """);

    [Fact]
    public Task GroupingSplitChainWithGroupByVisible_MySql_ReportsSqla0102() =>
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

    // Grouping() as a Where() argument never reaches a recognized clause anchor
    // (Select/Having/OrderBy), so the rule exits before it looks for a GroupBy —
    // regardless of the surrounding query nesting, IN, or AND shown here.
    [Fact]
    public Task GroupingInWhereOfDifferentQuery_MySql_StaysSilent() =>
        RunSilent("""
            var outer = Select(t.Dep).From(t).GroupBy(t.Dep).Having(
                t.Id.In(Select(s.Dep).From(s).Where(Grouping(s.Dep) == 0).GroupBy(s.Dep).WithRollup().Having(s.Dep > 0))
                & (t.Dep > 0));
            """);

    [Fact]
    public Task PercentileContWithoutOver_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:PercentileCont(0.5)|}.WithinGroup(OrderBy(t.Id))).From(t);
            """, "sqlserver");

    [Fact]
    public Task PercentileDiscWithoutOver_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:PercentileDisc(0.5)|}.WithinGroup(OrderBy(t.Id))).From(t);
            """, "sqlserver");

    [Fact]
    public Task PercentileContAliasedWithoutOver_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:PercentileCont(0.5)|}.WithinGroup(OrderBy(t.Id)).As("p")).From(t);
            """, "sqlserver");

    [Fact]
    public Task PercentileContWithOver_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(t.Id)).Over()).From(t);
            """, "sqlserver");

    [Fact]
    public Task PercentileContWithPartitionedOver_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(t.Id)).Over(PartitionBy(t.Dep))).From(t);
            """, "sqlserver");

    [Fact]
    public Task PercentileContWithoutOver_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(t.Id))).From(t);
            """, "postgresql");

    [Fact]
    public Task PercentileContWithoutOver_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(t.Id))).From(t);
            """, dbms: null);

    [Fact]
    public Task PercentileNestedInFunctionWithOver_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(Coalesce(PercentileCont(0.5).WithinGroup(OrderBy(t.Id)).Over(), 0)).From(t);
            """, "sqlserver");

    // The receiver leaves the expression, so a later .Over() is invisible (ADR 0003).
    [Fact]
    public Task PercentileContViaVariable_SqlServer_StaysSilent() =>
        RunSilent("""
            var p = PercentileCont(0.5).WithinGroup(OrderBy(t.Id));
            var q = Select(p.Over()).From(t);
            """, "sqlserver");

    [Fact]
    public Task InsertedInOutput_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Output(Inserted(t.Id)).Values(1);
            """, "sqlserver");

    [Fact]
    public Task DeletedInOutput_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Output(Deleted(t.Id));
            """, "sqlserver");

    [Fact]
    public Task InsertedAliasedInOutput_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Output(Inserted(t.Id).As("i")).Values(1);
            """, "sqlserver");

    [Fact]
    public Task InsertedInSelectList_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Inserted(t.Id)|}).From(t);
            """, "sqlserver");

    [Fact]
    public Task DeletedInSelectList_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Deleted(t.Id)|}).From(t);
            """, "sqlserver");

    [Fact]
    public Task InsertedInWhere_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(t.Id).From(t).Where({|#0:Inserted(t.Id)|} == 1);
            """, "sqlserver");

    [Fact]
    public Task InsertedOutsideOutput_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(Inserted(t.Id)).From(t);
            """, dbms: null);

    [Fact]
    public Task InsertedViaVariable_SqlServer_StaysSilent() =>
        RunSilent("""
            var i = Inserted(t.Id);
            var q = InsertInto(t, t.Id).Output(i).Values(1);
            """, "sqlserver");

    [Fact]
    public Task InsertedNestedInFunctionInsideOutput_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Output(Coalesce(Inserted(t.Id), 0)).Values(1);
            """, "sqlserver");

    [Fact]
    public Task IntervalBareSelectItem_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Interval(30, DateTimePart.Day)|}).From(t);
            """);

    [Fact]
    public Task IntervalLiteralArity2BareSelectItem_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:IntervalLiteral("30", DateTimePart.Day)|}).From(t);
            """);

    [Fact]
    public Task IntervalBareAmongMultipleSelectItems_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(t.Id, {|#0:Interval(30, DateTimePart.Day)|}).From(t);
            """);

    [Fact]
    public Task IntervalAsSubtractionOperand_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id - Interval(30, DateTimePart.Day)).From(t);
            """);

    [Fact]
    public Task IntervalAsAdditionOperand_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id + Interval(30, DateTimePart.Day)).From(t);
            """);

    [Fact]
    public Task IntervalAsLeftOperandOfAddition_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(Interval(30, DateTimePart.Day) + t.Id).From(t);
            """);

    [Fact]
    public Task IntervalLiteralArity2AsSubtractionOperand_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id - IntervalLiteral("30", DateTimePart.Day)).From(t);
            """);

    // The receiver leaves the expression at the point of the Interval(...) call, so
    // the +/- it later feeds is invisible to the walk — silent is the safe call
    // (ADR 0003: a false negative here, never a false positive on this valid SQL).
    [Fact]
    public Task IntervalViaVariable_MySql_StaysSilent() =>
        RunSilent("""
            var i = Interval(30, DateTimePart.Day);
            var q = Select(t.Id - i).From(t);
            """);

    [Fact]
    public Task IntervalBareSelectItem_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(Interval(30, DateTimePart.Day)).From(t);
            """, dbms: null);
}
