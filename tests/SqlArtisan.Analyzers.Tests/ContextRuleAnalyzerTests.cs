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
            public T() : base("t", "") { Id =
                new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
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
            test.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(0));
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
            var q = Select(t.Id).From(t).Where(
                t.Id.In(Select(s.Id).From(s).OrderBy(s.Id).Limit(2)));
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
            var q = Select(t.Id).From(t).Where(
                t.Id.In(Select(Grouping(s.Dep)).From(s).GroupBy(s.Dep).WithRollup()));
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
    public Task FirstValueOverPartitionOnly_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:FirstValue(t.Id).Over(PartitionBy(t.Dep))|}).From(t);
            """, "sqlserver");

    [Fact]
    public Task LastValueOverPartitionOnly_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:LastValue(t.Id).Over(PartitionBy(t.Dep))|}).From(t);
            """, "sqlserver");

    [Fact]
    public Task FirstValueOverEmpty_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:FirstValue(t.Id).Over()|}).From(t);
            """, "sqlserver");

    [Fact]
    public Task LastValueOverEmpty_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:LastValue(t.Id).Over()|}).From(t);
            """, "sqlserver");

    // The aggregate Over() shares the rule's name and arity; SQL Server runs it.
    [Fact]
    public Task AggregateOverEmpty_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(Sum(t.Id).Over()).From(t);
            """, "sqlserver");

    [Fact]
    public async Task NthValueOverEmpty_SqlServer_ReportsSqla0100Only()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = Select({|#0:NthValue(t.Id, 2)|}.Over()).From(t);
                """),
            AnalyzerVerifier.EditorConfig("sqlserver"));

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public Task FirstValueOverEmpty_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(FirstValue(t.Id).Over()).From(t);
            """, "postgresql");

    // A base-typed receiver no longer names which function it is, and NthValue's
    // failure there is not the window shape — so the rule goes silent (ADR 0003).
    [Fact]
    public Task FirstValueOverPartitionOnlyViaBaseVariable_SqlServer_StaysSilent() =>
        RunSilent("""
            ValueAnalyticFunction f = FirstValue(t.Id);
            var q = Select(f.Over(PartitionBy(t.Dep))).From(t);
            """, "sqlserver");

    [Fact]
    public async Task NthValueOverPartitionOnlyViaBaseVariable_SqlServer_ReportsSqla0100Only()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                ValueAnalyticFunction f = {|#0:NthValue(t.Id, 2)|};
                var q = Select(f.Over(PartitionBy(t.Dep))).From(t);
                """),
            AnalyzerVerifier.EditorConfig("sqlserver"));

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public Task FirstValueOverPartitionByOrderBy_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(FirstValue(t.Id).Over(PartitionBy(t.Dep).OrderBy(t.Id))).From(t);
            """, "sqlserver");

    [Fact]
    public Task FirstValueOverFrame_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(FirstValue(t.Id).Over(OrderBy(t.Id).Rows(UnboundedPreceding))).From(t);
            """, "sqlserver");

    // The aggregate and percentile Over(PartitionByClause) overloads share the
    // rule's name and parameter type; only the value family's is restricted.
    [Fact]
    public Task AggregateOverPartitionOnly_SqlServer_StaysSilent() =>
        RunSilent("""
            var q = Select(Sum(t.Id).Over(PartitionBy(t.Dep))).From(t);
            """, "sqlserver");

    // SQL Server has no NTH_VALUE at all, so SQLA0100 is the whole verdict —
    // naming the window shape would misname why it fails there.
    [Fact]
    public async Task NthValueOverPartitionOnly_SqlServer_ReportsSqla0100Only()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = Select({|#0:NthValue(t.Id, 2)|}.Over(PartitionBy(t.Dep))).From(t);
                """),
            AnalyzerVerifier.EditorConfig("sqlserver"));

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public Task FirstValueOverPartitionOnly_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(FirstValue(t.Id).Over(PartitionBy(t.Dep))).From(t);
            """, "postgresql");

    [Fact]
    public Task FirstValueOverPartitionOnly_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(FirstValue(t.Id).Over(PartitionBy(t.Dep))).From(t);
            """, dbms: null);

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
            var q = Select({|#0:IntervalLiteral("30", Day())|}).From(t);
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
            var q = Select(t.Id - IntervalLiteral("30", Day())).From(t);
            """);

    [Fact]
    public Task IntervalAsDateAddArgument_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(DateAdd(t.Id, Interval(30, DateTimePart.Day))).From(t);
            """);

    [Fact]
    public Task IntervalAsDateSubArgument_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(DateSub(t.Id, Interval(30, DateTimePart.Day))).From(t);
            """);

    [Fact]
    public Task IntervalLiteralArity2AsDateAddArgument_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(DateAdd(t.Id, IntervalLiteral("30", Day()))).From(t);
            """);

    // The interval slot is exempt (above), but the date slot is not — DATE_ADD's
    // first argument must be a date expression, so INTERVAL there is still bare.
    [Fact]
    public Task IntervalAsDateAddDateArgument_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(DateAdd({|#0:Interval(30, DateTimePart.Day)|}, Interval(3, DateTimePart.Month))).From(t);
            """);

    [Fact]
    public Task IntervalAsDateSubDateArgument_MySql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(DateSub({|#0:Interval(30, DateTimePart.Day)|}, Interval(3, DateTimePart.Month))).From(t);
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

    [Theory]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public Task JoinedDeleteFrom_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:DeleteFrom(t).From(t, s)|}.Where(t.Dep == s.Id);
            """, dbms);

    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public Task JoinedDeleteFrom_StaysSilent(string dbms) =>
        RunSilent("""
            var q = DeleteFrom(t).From(t, s).Where(t.Dep == s.Id);
            """, dbms);

    [Fact]
    public Task JoinedDeleteFromWithJoin_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:DeleteFrom(t).From(t)|}.InnerJoin(s).On(t.Dep == s.Id);
            """, "postgresql");

    // The declaring interface is the whole proof, so — unlike the walking rules —
    // parking the builder in a variable hides nothing.
    [Fact]
    public Task JoinedDeleteFromViaVariable_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var d = DeleteFrom(t);
            var q = {|#0:d.From(t, s)|}.Where(t.Dep == s.Id);
            """, "postgresql");

    [Fact]
    public Task JoinedDeleteFrom_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).From(t, s).Where(t.Dep == s.Id);
            """, dbms: null);

    [Fact]
    public Task SelectFrom_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).InnerJoin(s).On(t.Dep == s.Id);
            """, "postgresql");

    [Fact]
    public Task DeleteUsing_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:DeleteFrom(t).Using(s)|}.Where(t.Dep == s.Id);
            """, "oracle");

    // Oracle 23ai added DELETE ... USING; the baseline (21) still reports, as above.
    [Fact]
    public Task DeleteUsing_Oracle_BelowFloor_ReportsSqla0102() =>
        RunAsync(
            Usage("""
                var q = {|#0:DeleteFrom(t).Using(s)|}.Where(t.Dep == s.Id);
                """),
            AnalyzerVerifier.EditorConfig("oracle", "21"),
            expectWarning: true);

    [Fact]
    public Task DeleteUsing_Oracle_AtFloor_StaysSilent() =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage("""
                var q = DeleteFrom(t).Using(s).Where(t.Dep == s.Id);
                """)),
            AnalyzerVerifier.EditorConfig("oracle", "23"),
            expectWarning: false);

    [Fact]
    public Task DeleteUsing_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Using(s).Where(t.Dep == s.Id);
            """, "postgresql");

    // The other two Using overloads: the JOIN ... USING(column) list and MERGE's
    // source, neither of which is the DELETE ... USING clause. The column list is
    // reachable only after From(...), so it is checked where the lead itself parses.
    [Fact]
    public Task DeleteJoinUsingColumns_MySql_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).From(t).InnerJoin(s).Using(t.Id);
            """);

    [Fact]
    public Task MergeUsing_Oracle_StaysSilent() =>
        RunSilent("""
            var q = MergeInto(t).Using(s).On(t.Id == s.Id).WhenMatched()
                .ThenUpdateSet(t.Dep == s.Dep);
            """, "oracle");

    [Theory]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public Task JoinedUpdateJoin_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:Update(t).InnerJoin(s)|}.On(t.Dep == s.Id).Set(t.Id == s.Id);
            """, dbms);

    [Fact]
    public Task JoinedUpdateJoin_MySql_StaysSilent() =>
        RunSilent("""
            var q = Update(t).InnerJoin(s).On(t.Dep == s.Id).Set(t.Id == s.Id);
            """);

    [Fact]
    public Task JoinedUpdateLeftJoin_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:Update(t).LeftJoin(s)|}.On(t.Dep == s.Id).Set(t.Id == s.Id);
            """, "postgresql");

    // The second join hangs off IUpdateBuilderJoined, the other interface that
    // declares the direct-join steps — and is the same defect as the first, so
    // both are reported.
    [Fact]
    public async Task JoinedUpdateSecondJoin_PostgreSql_ReportsSqla0102PerJoin()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = {|#0:Update(t).InnerJoin(s)|}.On(t.Dep == s.Id);
                var r = {|#1:q.RightJoin(s)|}.On(t.Id == s.Id).Set(t.Id == s.Id);
                """),
            AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(0));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(1));

        await test.RunAsync();
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("oracle")]
    public Task JoinedUpdateFrom_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:Update(t).Set(t.Id == s.Id).From(s)|}.Where(t.Dep == s.Id);
            """, dbms);

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public Task JoinedUpdateFrom_StaysSilent(string dbms) =>
        RunSilent("""
            var q = Update(t).Set(t.Id == s.Id).From(s).Where(t.Dep == s.Id);
            """, dbms);

    // Oracle 23ai and SQLite 3.33 added the FROM form: a declared version at or past
    // that floor is silent, one below it reports, and none reads the matrix baseline.
    [Theory]
    [InlineData("oracle", "21")]
    [InlineData("sqlite", "3.32")]
    public Task JoinedUpdateFrom_BelowFloor_ReportsSqla0102(string dbms, string version) =>
        RunAsync(
            Usage("""
                var q = {|#0:Update(t).Set(t.Id == s.Id).From(s)|}.Where(t.Dep == s.Id);
                """),
            AnalyzerVerifier.EditorConfig(dbms, version),
            expectWarning: true);

    [Theory]
    [InlineData("oracle", "23")]
    [InlineData("sqlite", "3.33")]
    public Task JoinedUpdateFrom_AtFloor_StaysSilent(string dbms, string version) =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage("""
                var q = Update(t).Set(t.Id == s.Id).From(s).Where(t.Dep == s.Id);
                """)),
            AnalyzerVerifier.EditorConfig(dbms, version),
            expectWarning: false);

    [Fact]
    public Task SecondValuesRow_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:InsertInto(t, t.Id).Values(1).Values(2)|};
            """, "oracle");

    [Fact]
    public Task SecondValuesRow_Oracle23_StaysSilent() =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage("""
                var q = InsertInto(t, t.Id).Values(1).Values(2);
                """)),
            AnalyzerVerifier.EditorConfig("oracle", "23"),
            expectWarning: false);

    // One row, or a collection whose row count is a value the stage cannot prove.
    [Theory]
    [InlineData("var q = InsertInto(t, t.Id).Values(1);")]
    [InlineData("var q = InsertInto(t, t.Id).Values(new[] { new object[] { 1 } });")]
    public Task FirstValuesRow_Oracle_StaysSilent(string statement) =>
        RunSilent(statement, "oracle");

    [Fact]
    public Task SecondValuesRow_MySql_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Values(1).Values(2);
            """);

    // A join reached through From(...) is the FROM-form's own join, not the
    // direct-join spelling the other rule reports.
    [Fact]
    public Task JoinedUpdateFromJoin_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Id == s.Id).From(s).InnerJoin(s).On(t.Dep == s.Id);
            """, "postgresql");

    [Fact]
    public Task InsertSelectWith_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            Cte c = new Cte("c");
            var q = {|#0:InsertInto(t, t.Id).With(c.As(Select(s.Id).From(s)))|}
                .Select(c.Column(s.Id)).From(c);
            """, "sqlserver");

    [Fact]
    public Task InsertSelectWithViaVariable_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            Cte c = new Cte("c");
            var i = InsertInto(t, t.Id);
            var q = {|#0:i.With(c.As(Select(s.Id).From(s)))|}.Select(c.Column(s.Id)).From(c);
            """, "sqlserver");

    [Theory]
    [InlineData("mysql")]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public Task InsertSelectWith_StaysSilent(string dbms) =>
        RunSilent("""
            Cte c = new Cte("c");
            var q = InsertInto(t, t.Id).With(c.As(Select(s.Id).From(s)))
                .Select(c.Column(s.Id)).From(c);
            """, dbms);

    // The leading WITH is SQL Server's own spelling of the same statement.
    [Fact]
    public Task LeadingWithInsertSelect_SqlServer_StaysSilent() =>
        RunSilent("""
            Cte c = new Cte("c");
            var q = With(c.As(Select(s.Id).From(s))).InsertInto(t, t.Id)
                .Select(c.Column(s.Id)).From(c);
            """, "sqlserver");

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public Task MergeUpdateActionWhere_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Dep == s.Dep).Where(t.Id == 1)|};
            """, dbms);

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public Task MergeInsertActionWhere_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenNotMatched().ThenInsert(t.Id).Values(s.Id).Where(s.Id == 1)|};
            """, dbms);

    [Fact]
    public Task MergeActionWhereViaVariable_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var u = MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Dep == s.Dep);
            var q = {|#0:u.Where(t.Id == 1)|};
            """, "postgresql");

    [Fact]
    public Task MergeActionWhere_Oracle_StaysSilent() =>
        RunSilent("""
            var q = MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Dep == s.Dep).Where(t.Id == 1)
                .WhenNotMatched().ThenInsert(t.Id).Values(s.Id).Where(s.Id == 1);
            """, "oracle");

    [Fact]
    public async Task MergeBranchAfterUnconditioned_ReportsEveryMergeDialect()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenMatched().ThenUpdateSet(t.Dep == s.Dep)
                    .WhenMatched()|}.ThenUpdateSet(t.Id == s.Id);
                """),
            """
            root = true

            [*.cs]
            sqlartisan_syntax_oracle = any
            sqlartisan_syntax_postgresql = any
            sqlartisan_syntax_sqlserver = any
            """);
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0102")
            .WithLocation(0)
            .WithArguments(
                "WhenMatched",
                "after an unconditioned WHEN MATCHED branch",
                "Oracle, PostgreSQL and SQL Server"));

        await test.RunAsync();
    }

    [Fact]
    public async Task MergeBranchSameAction_SqlServer_ReportsSqla0102()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenMatched(s.Id > 1).ThenUpdateSet(t.Dep == s.Dep)
                    .WhenMatched(s.Id > 2)|}.ThenUpdateSet(t.Dep == s.Dep);
                """),
            AnalyzerVerifier.EditorConfig("sqlserver"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0102")
            .WithLocation(0)
            .WithArguments(
                "WhenMatched", "after a WHEN MATCHED branch with the same action", "SQL Server"));

        await test.RunAsync();
    }

    // WHEN NOT MATCHED has one action, so any second one repeats it.
    [Fact]
    public Task MergeNotMatchedAfterConditioned_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenNotMatched(s.Id > 1).ThenInsert(t.Id).Values(s.Id)
                .WhenNotMatched()|}.ThenInsert(t.Id).Values(s.Id);
            """, "sqlserver");

    [Fact]
    public Task MergeNotMatchedRepeated_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenNotMatched().ThenInsert(t.Id).Values(s.Id)
                .WhenNotMatched()|}.ThenInsert(t.Id).Values(s.Id);
            """, "oracle");

    [Theory]
    [InlineData("sqlserver", "any")]
    [InlineData("postgresql", "17")]
    public Task MergeBySourceAfterUnconditioned_ReportsSqla0102(string dbms, string version) =>
        RunAsync(
            Usage("""
                var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenNotMatchedBySource().ThenUpdateSet(t.Dep == 0)
                    .WhenNotMatchedBySource()|}.ThenDelete();
                """),
            AnalyzerVerifier.EditorConfig(dbms, version),
            expectWarning: true);

    // Below 17 the matrix already reports both BY SOURCE calls; the rule adds nothing.
    [Fact]
    public async Task MergeBySourceAfterUnconditioned_PostgreSql16_ReportsSqla0100Only()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                var q = {|#1:{|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenNotMatchedBySource()|}.ThenUpdateSet(t.Dep == 0)
                    .WhenNotMatchedBySource()|}.ThenDelete();
                """),
            AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(0));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0100").WithLocation(1));

        await test.RunAsync();
    }

    // An override that silences SQLA0100 hands the repeat back to this rule.
    [Fact]
    public Task MergeBySourceAfterUnconditioned_PostgreSql16Overridden_ReportsSqla0102() =>
        RunAsync(
            Usage("""
                var q = {|#0:MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenNotMatchedBySource().ThenUpdateSet(t.Dep == 0)
                    .WhenNotMatchedBySource()|}.ThenDelete();
                """),
            """
            root = true

            [*.cs]
            sqlartisan_syntax_postgresql = any
            sqlartisan_construct_when_not_matched_by_source = supported
            """,
            expectWarning: true);

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public Task MergeBranchAfterConditioned_StaysSilent(string dbms) =>
        RunSilent("""
            var q = MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched(s.Id > 1).ThenUpdateSet(t.Dep == s.Dep)
                .WhenMatched().ThenDelete()
                .WhenNotMatched().ThenInsert(t.Id).Values(s.Id);
            """, dbms);

    [Theory]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public Task MergeOneBranchPerClause_StaysSilent(string dbms) =>
        RunSilent("""
            var q = MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Dep == s.Dep)
                .WhenNotMatched().ThenInsert(t.Id).Values(s.Id);
            """, dbms);

    // The rule reads back up the chain, so an earlier branch held in a variable is out
    // of sight (ADR 0003).
    [Fact]
    public Task MergeRepeatedBranchViaVariable_Oracle_StaysSilent() =>
        RunSilent("""
            var m = MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Dep == s.Dep);
            var q = m.WhenMatched().ThenUpdateSet(t.Id == s.Id);
            """, "oracle");

    // The other stages declaring Where filter a statement or an upsert action, not MERGE.
    [Theory]
    [InlineData("var q = Select(t.Id).From(t).Where(t.Id == 1);")]
    [InlineData("var q = Update(t).Set(t.Dep == 1).Where(t.Id == 1);")]
    [InlineData("var q = DeleteFrom(t).Where(t.Id == 1);")]
    [InlineData("var q = InsertInto(t, t.Id).Values(1).OnConflict(t.Id)"
        + ".DoUpdateSet(t.Dep == 1).Where(t.Id == 1);")]
    public Task StatementWhere_PostgreSql_StaysSilent(string statement) =>
        RunSilent(statement, "postgresql");

    [Fact]
    public Task ReturningThenBuild_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:InsertInto(t, t.Id).Values(1).Returning(t.Id)|}.Build();
            """, "oracle");

    [Theory]
    [InlineData("var q = {|#0:Update(t).Set(t.Dep == 1).Returning(t.Id)|}.Build(Dbms.Oracle);")]
    [InlineData("ISqlBuilder q = {|#0:DeleteFrom(t).Where(t.Id == 1).Returning(t.Id)|};")]
    [InlineData("""
        static void Run(ISqlBuilder b) { }
        Run({|#0:InsertInto(t, t.Id).Values(1).Returning(t.Id)|});
        """)]
    public Task ReturningConsumedWithoutInto_Oracle_ReportsSqla0102(string statements) =>
        RunReporting(statements, "oracle");

    [Fact]
    public Task ReturningInto_Oracle_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Values(1).Returning(t.Id)
                .Into(new OutputParameter("id", System.Data.DbType.Int32));
            """, "oracle");

    // A result held as IReturningBuilder can still take Into on a later line.
    [Theory]
    [InlineData("var r = InsertInto(t, t.Id).Values(1).Returning(t.Id);")]
    [InlineData("""
        static void Run(IReturningBuilder b) { }
        Run(InsertInto(t, t.Id).Values(1).Returning(t.Id));
        """)]
    public Task ReturningHeldForInto_Oracle_StaysSilent(string statements) =>
        RunSilent(statements, "oracle");

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public Task ReturningThenBuild_StaysSilent(string dbms) =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Values(1).Returning(t.Id).Build();
            """, dbms);

    [Theory]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    public Task ForUpdateAfterGroupBy_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = {|#0:Select(t.Dep).From(t).GroupBy(t.Dep).OrderBy(t.Dep).ForUpdate()|};
            """, dbms);

    // MySQL locks the grouped query's base rows rather than rejecting it.
    [Fact]
    public Task ForUpdateAfterGroupBy_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep).From(t).GroupBy(t.Dep).OrderBy(t.Dep).ForUpdate();
            """);

    [Fact]
    public Task ForUpdateAfterGroupByHaving_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:Select(t.Dep).From(t).GroupBy(t.Dep).Having(t.Dep > 0)
                .OrderBy(t.Dep).ForUpdate()|};
            """, "postgresql");

    [Fact]
    public Task ForUpdateWithoutGroupBy_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep).From(t).OrderBy(t.Dep).ForUpdate();
            """, "postgresql");

    // The GroupBy sits in a subquery argument, not in ForUpdate's receiver chain.
    [Fact]
    public Task ForUpdateWithGroupedSubquery_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t)
                .Where(t.Id.In(Select(s.Dep).From(s).GroupBy(s.Dep))).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateAfterGroupByViaVariable_PostgreSql_StaysSilent() =>
        RunSilent("""
            var g = Select(t.Dep).From(t).GroupBy(t.Dep);
            var q = g.OrderBy(t.Dep).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateAfterGroupBy_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Dep).From(t).GroupBy(t.Dep).OrderBy(t.Dep).ForUpdate();
            """, dbms: null);

    [Fact]
    public Task ForUpdateAfterFetchFirst_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:Select(t.Id).From(t).OrderBy(t.Id).FetchFirst(1).ForUpdate()|};
            """, "oracle");

    [Fact]
    public Task ForUpdateAfterOffsetRows_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:Select(t.Id).From(t).OrderBy(t.Id).OffsetRows(1).ForUpdate()|};
            """, "oracle");

    [Fact]
    public Task ForUpdateAfterOffsetRowsFetchNext_Oracle_ReportsSqla0102() =>
        RunReporting("""
            var q = {|#0:Select(t.Id).From(t).OrderBy(t.Id).OffsetRows(1).FetchNext(1)
                .ForUpdate()|};
            """, "oracle");

    // PostgreSQL 16 runs the row-limiting clause and the lock together.
    [Fact]
    public Task ForUpdateAfterFetchFirst_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).OrderBy(t.Id).FetchFirst(1).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateAfterLimit_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).OrderBy(t.Id).Limit(1).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateWithoutRowLimiting_Oracle_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).OrderBy(t.Id).ForUpdate();
            """, "oracle");

    // The FetchFirst sits in a subquery argument, not in ForUpdate's receiver chain.
    [Fact]
    public Task ForUpdateWithRowLimitedSubquery_Oracle_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t)
                .Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep).FetchFirst(2))).ForUpdate();
            """, "oracle");

    [Fact]
    public Task ForUpdateAfterFetchFirstViaVariable_Oracle_StaysSilent() =>
        RunSilent("""
            var p = Select(t.Id).From(t).OrderBy(t.Id).FetchFirst(1);
            var q = p.ForUpdate();
            """, "oracle");

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Dep).From(s).ForUpdate()|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(Exists({|#0:Select(s.Dep).From(s).ForUpdate()|}));
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As({|#0:Select(s.Dep).From(s).ForUpdate()|})).Select(c.Column("dep")).From(c);
        """)]
    [InlineData("""
        var d = {|#0:Select(s.Dep).From(s).ForUpdate(SkipLocked)|}.AsTable("d");
        """)]
    [InlineData("""
        var q = Select({|#0:Select(s.Dep).From(s).ForUpdate()|}.As("m")).From(t);
        """)]
    [InlineData("""
        ISubquery q = {|#0:Select(s.Dep).From(s).ForUpdate()|};
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id == {|#0:Select(s.Dep).From(s).ForUpdate()|});
        """)]
    [InlineData("""
        var q = Select({|#0:Select(s.Dep).From(s).ForUpdate()|}).From(t);
        """)]
    [InlineData("""
        var q = Select(t.Id, {|#0:Select(s.Dep).From(s).ForUpdate()|}).From(t);
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As(Select(t.Id).From(t)))
            .Select({|#0:Select(s.Dep).From(s).ForUpdate()|}).From(c);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .CrossApply({|#0:Select(s.Dep).From(s).ForUpdate()|}, new DerivedTable("x"));
        """)]
    public Task ForUpdateInSubquery_Oracle_ReportsSqla0102(string statements) =>
        RunReporting(statements, "oracle");

    // MySQL 8.0 and PostgreSQL 16 run a locked subquery, CTE body and derived table.
    [Theory]
    [InlineData("mysql")]
    [InlineData("postgresql")]
    public Task ForUpdateInSubquery_StaysSilent(string dbms) =>
        RunSilent("""
            var q = Select(t.Id).From(t).Where(t.Id.In(Select(s.Dep).From(s).ForUpdate()));
            """, dbms);

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).ForUpdate().Build();
        """)]
    [InlineData("""
        object q = Select(t.Id).From(t).ForUpdate();
        """)]
    [InlineData("""
        var q = System.Convert.ToString(Select(t.Id).From(t).ForUpdate());
        """)]
    [InlineData("""
        var q = t.Id.Equals(Select(s.Dep).From(s).ForUpdate());
        """)]
    // Object positions outside the named hosts are not read, whatever they embed.
    [InlineData("""
        var q = Select(Nvl(Select(s.Dep).From(s).ForUpdate(), 0)).From(t);
        """)]
    public Task ForUpdateNotAsSubquery_Oracle_StaysSilent(string statements) =>
        RunSilent(statements, "oracle");

    // The conversion happens where the variable is read, out of the rule's sight.
    [Fact]
    public Task ForUpdateInSubqueryViaVariable_Oracle_StaysSilent() =>
        RunSilent("""
            var locked = Select(s.Dep).From(s).ForUpdate();
            var q = Select(t.Id).From(t).Where(t.Id.In(locked));
            """, "oracle");
}
