using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class ContextRuleAnalyzerTests
{
    // The marked span is the whole trigger invocation (receiver chain included) —
    // the same location SQLA0100 reports for an instance-chain member. The UNION
    // sort-key rule marks each key instead, since one OrderBy can hold several.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Dep;
            public T() : base("t", "") { Id =
                new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
        }

        class A : DbTableBase
        {
            public DbColumn Id;
            public A(string alias = "") : base("a", alias) { Id = new DbColumn(this, "id"); }
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

    [Theory]
    [InlineData("PercentileCont(0.5).WithinGroup(OrderBy(t.Id)).Over()")]
    [InlineData("PercentileDisc(0.5).WithinGroup(OrderBy(t.Id)).Over(PartitionBy(t.Dep))")]
    public Task PercentileOver_PostgreSql_ReportsSqla0102(string percentile) =>
        RunReporting($$"""
            var q = Select({|#0:{{percentile}}|}).From(t);
            """, "postgresql");

    // The percentile is held first, so only the Over call is in view; it is still on one.
    [Fact]
    public Task PercentileOverViaVariable_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var p = PercentileCont(0.5).WithinGroup(OrderBy(t.Id));
            var q = Select({|#0:p.Over()|}).From(t);
            """, "postgresql");

    [Fact]
    public Task PercentileOver_Oracle_StaysSilent() =>
        RunSilent("""
            var q = Select(PercentileCont(0.5).WithinGroup(OrderBy(t.Id)).Over()).From(t);
            """, "oracle");

    [Fact]
    public Task AggregateOver_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(Sum(t.Id).Over(PartitionBy(t.Dep))).From(t);
            """, "postgresql");

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

    [Theory]
    [InlineData("mysql")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    [InlineData("sqlserver")]
    public Task CountDistinctOverEmpty_ReportsSqla0102(string dbms) =>
        RunReporting("""
            var q = Select({|#0:Count(Distinct, t.Id).Over()|}).From(t);
            """, dbms);

    [Fact]
    public Task SumDistinctOverPartitionOnly_PostgreSql_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Sum(Distinct, t.Id).Over(PartitionBy(t.Dep))|}).From(t);
            """, "postgresql");

    [Fact]
    public Task AvgDistinctOverEmpty_Sqlite_ReportsSqla0102() =>
        RunReporting("""
            var q = Select({|#0:Avg(Distinct, t.Id).Over()|}).From(t);
            """, "sqlite");

    [Fact]
    public Task CountDistinctOverPartitionOnly_Oracle_StaysSilent() =>
        RunSilent("""
            var q = Select(Count(Distinct, t.Id).Over(PartitionBy(t.Dep))).From(t);
            """, "oracle");

    [Fact]
    public Task CountDistinctFilter_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(Count(Distinct, t.Id).Filter(t.Dep > 0)).From(t);
            """, "postgresql");

    [Fact]
    public Task CountDistinctOverEmpty_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = Select(Count(Distinct, t.Id).Over()).From(t);
            """, dbms: null);

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
    public Task InsertedWrappedViaVariable_SqlServer_StaysSilent() =>
        RunSilent("""
            var e = Coalesce(Inserted(t.Id), 0);
            var q = Update(t).Set(t.Id == 1).Output(e);
            """, "sqlserver");

    [Fact]
    public Task InsertedWrappedInTernary_SqlServer_StaysSilent() =>
        RunSilent("""
            bool f = true;
            var q = Update(t).Set(t.Id == 1).Output(f ? Coalesce(Inserted(t.Id), 0) : t.Id);
            """, "sqlserver");

    [Fact]
    public Task InsertedWrappedThroughHelper_SqlServer_StaysSilent() =>
        RunSilent("""
            static SqlExpression Keep(SqlExpression e) => e;
            var q = Update(t).Set(t.Id == 1).Output(Keep(Coalesce(Inserted(t.Id), 0)));
            """, "sqlserver");

    [Fact]
    public Task InsertedWrappedInLambda_SqlServer_StaysSilent() =>
        RunSilent("""
            System.Func<SqlExpression> f = () => Coalesce(Inserted(t.Id), 0);
            var q = Update(t).Set(t.Id == 1).Output(f());
            """, "sqlserver");

    [Fact]
    public Task InsertedWrappedInHeldArray_SqlServer_StaysSilent() =>
        RunSilent("""
            object[] items = [Coalesce(Inserted(t.Id), 0)];
            var q = Update(t).Set(t.Id == 1).Output(items);
            """, "sqlserver");

    [Fact]
    public Task InsertedWrappedInSelectList_SqlServer_ReportsSqla0102() =>
        RunReporting("""
            var q = Select(Coalesce({|#0:Inserted(t.Id)|}, 0)).From(t);
            """, "sqlserver");

    // Written straight into a statement step, the pseudo-table cannot reach an
    // Output, however the builder before that step was held.
    [Theory]
    [InlineData("var u = Update(t).Set(t.Dep == 1); var q = u.Where({|#0:Inserted(t.Id)|} == 1);")]
    [InlineData("var u = Update(t); var q = u.Set(t.Dep == {|#0:Inserted(t.Id)|});")]
    [InlineData("""var u = Select(t.Id).From(t); var q = u.Where(t.Id == Coalesce({|#0:Deleted(t.Id)|}, 0));""")]
    public Task PseudoTableIntoHeldBuilderStep_SqlServer_ReportsSqla0102(string statements) =>
        RunReporting(statements, "sqlserver");

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

    // Oracle 23.26.3 runs the lead that does not re-list the target and rejects the
    // re-listed one (ORA-00942); XE 21.3.0 rejects both (#679).
    [Theory]
    [InlineData("var q = {|#0:DeleteFrom(t).From(s)|}.Where(t.Dep == s.Id);")]
    [InlineData("var q = {|#0:DeleteFrom(t).From(t, s)|}.Where(t.Dep == s.Id);")]
    public Task JoinedDeleteFrom_Oracle_BelowFloor_ReportsSqla0102(string statement) =>
        RunAsync(
            Usage(statement),
            AnalyzerVerifier.EditorConfig("oracle", "21"),
            expectWarning: true);

    [Theory]
    [InlineData("var q = {|#0:DeleteFrom(t).From(t, s)|}.Where(t.Dep == s.Id);")]
    [InlineData("var q = {|#0:DeleteFrom(t).From(s)|}.InnerJoin(t).On(t.Dep == s.Id);")]
    public Task JoinedDeleteFrom_Oracle_AtFloorRelisted_ReportsSqla0102(string statement) =>
        RunAsync(
            Usage(statement),
            AnalyzerVerifier.EditorConfig("oracle", "23"),
            expectWarning: true);

    // Past the floor only a provable re-list reports: the lead that does not re-list
    // runs, and a target the symbols cannot prove (a variable chain) stays silent.
    [Theory]
    [InlineData("var q = DeleteFrom(t).From(s).Where(t.Dep == s.Id);")]
    [InlineData("var u = t; var q = DeleteFrom(t).From(u, s).Where(t.Dep == s.Id);")]
    public Task JoinedDeleteFrom_Oracle_AtFloorNotProvablyRelisted_StaysSilent(string statement) =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statement)),
            AnalyzerVerifier.EditorConfig("oracle", "23"),
            expectWarning: false);

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
    [InlineData("InsertInto(t, t.Id).Values(1).Returning(t.Asterisk)")]
    [InlineData("Update(t).Set(t.Dep == 1).Where(t.Id == 1).Returning(t.Id, t.Asterisk)")]
    [InlineData("DeleteFrom(t).Where(t.Id == 1).Returning(t.Asterisk)")]
    public Task ReturningTableAsterisk_Sqlite_ReportsSqla0102(string statement) =>
        RunReporting($$"""
            var q = {|#0:{{statement}}|};
            """, "sqlite");

    [Fact]
    public Task ReturningAsterisk_Sqlite_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Where(t.Id == 1).Returning(Asterisk);
            """, "sqlite");

    [Fact]
    public Task ReturningTableAsterisk_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Where(t.Id == 1).Returning(t.Asterisk);
            """, "postgresql");

    [Theory]
    [InlineData("Returning(s.Id)")]
    [InlineData("Returning(t.Id, s.Dep + 1)")]
    [InlineData("Returning(Coalesce(s.Dep, 0).As(\"d\"))")]
    public Task ReturningJoinedTable_Sqlite_ReportsSqla0102(string returning) =>
        RunReporting($$"""
            var q = {|#0:Update(t).Set(t.Dep == 1).From(s).Where(s.Id == t.Id).{{returning}}|};
            """, "sqlite");

    // The subquery is a query block of its own, and the target is what RETURNING reads.
    [Theory]
    [InlineData("Returning(t.Id)")]
    [InlineData("Returning(Select(s.Dep).From(s).Where(s.Id == t.Id))")]
    public Task ReturningTargetOrSubquery_Sqlite_UpdateFrom_StaysSilent(string returning) =>
        RunSilent($$"""
            var q = Update(t).Set(t.Dep == 1).From(s).Where(s.Id == t.Id).{{returning}};
            """, "sqlite");

    // Build() rejects a RETURNING beside a re-listed target, so its message stands alone.
    [Theory]
    [InlineData("From(t)")]
    [InlineData("From(s, t)")]
    public Task ReturningAfterRelistedTarget_Sqlite_StaysSilent(string from) =>
        RunSilent($$"""
            var q = Update(t).Set(t.Dep == 1).{{from}}.Where(s.Id == t.Id).Returning(s.Id);
            """, "sqlite");

    [Fact]
    public Task ReturningJoinedTable_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Dep == 1).From(s).Where(s.Id == t.Id).Returning(s.Id);
            """, "postgresql");

    // The UPDATE ... FROM is held first, so its joined table is out of view.
    [Fact]
    public Task ReturningJoinedTableViaVariable_Sqlite_StaysSilent() =>
        RunSilent("""
            var u = Update(t).Set(t.Dep == 1).From(s).Where(s.Id == t.Id);
            var q = u.Returning(s.Id);
            """, "sqlite");

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

    // MySQL runs the lock on the last branch's rows alone, another meaning (#822).
    [Theory]
    [InlineData("mysql", "Union")]
    [InlineData("mysql", "UnionAll")]
    [InlineData("mysql", "Intersect")]
    [InlineData("mysql", "IntersectAll")]
    [InlineData("mysql", "Except")]
    [InlineData("mysql", "ExceptAll")]
    [InlineData("oracle", "Union")]
    [InlineData("postgresql", "Union")]
    [InlineData("postgresql", "UnionAll")]
    [InlineData("postgresql", "Intersect")]
    [InlineData("postgresql", "Except")]
    public Task ForUpdateAfterSetOperator_ReportsSqla0102(string dbms, string setOperator) =>
        RunReporting($$"""
            var q = {|#0:Select(t.Id).From(t).{{setOperator}}.Select(s.Id).From(s).Where(s.Id > 0)
                .ForUpdate()|};
            """, dbms);

    // The set operator sits in the subquery's own chain, not in ForUpdate's.
    [Fact]
    public Task ForUpdateWithSetOperatedSubquery_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t)
                .Where(t.Id.In(Select(s.Id).From(s).Union.Select(s.Dep).From(s))).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateAfterSetOperatorViaVariable_PostgreSql_StaysSilent() =>
        RunSilent("""
            var u = Select(t.Id).From(t).Union;
            var q = u.Select(s.Id).From(s).ForUpdate();
            """, "postgresql");

    [Theory]
    [InlineData("mysql", "Union", "a.Id")]
    [InlineData("oracle", "Union", "a.Id")]
    [InlineData("postgresql", "Union", "a.Id")]
    [InlineData("postgresql", "UnionAll", "a.Id")]
    [InlineData("postgresql", "Union", "a.Id.Desc")]
    [InlineData("postgresql", "Union", "a.Id.Asc.NullsFirst")]
    [InlineData("postgresql", "Union", "a.Id.NullsLast")]
    public Task QualifiedKeyAfterUnion_AliasedTable_ReportsSqla0102(
        string dbms, string setOperator, string key) =>
        RunReporting($$"""
            A a = new("a");
            A b = new A(alias: "b");
            var q = Select(a.Id).From(a).{{setOperator}}.Select(b.Id).From(b)
                .OrderBy({|#0:{{key}}|});
            """, dbms);

    [Theory]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As(Select(t.Id).From(t))).Select(c.Column("id")).From(c)
            .Union.Select(s.Id).From(s).OrderBy({|#0:c.Column("id")|});
        """)]
    [InlineData("""
        var d = Select(t.Id).From(t).AsTable("d");
        var q = Select(d.Column(t.Id)).From(d).UnionAll.Select(s.Id).From(s)
            .OrderBy({|#0:d.Column(t.Id).Desc|});
        """)]
    [InlineData("""
        DerivedTable x = new("x");
        var q = Select(t.Id).From(t).Union.Select(s.Id).From(s).OrderBy({|#0:x.Column("id")|});
        """)]
    [InlineData("""
        var q = Select(Asterisk).From(Select(t.Id).From(t).Union.Select(s.Id).From(s)
            .OrderBy({|#0:Values("v", ["id"], [[1]]).Column("id")|}).AsTable("z"));
        """)]
    public Task QualifiedKeyAfterUnion_PostgreSql_CteOrDerivedTable_ReportsSqla0102(
        string statements) =>
        RunReporting(statements, "postgresql");

    // A correlated compound can sort by the outer query's table, which no branch lists.
    [Fact]
    public Task QualifiedKeyAfterUnion_PostgreSql_OuterQueryTable_ReportsSqla0102() =>
        RunReporting("""
            A x = new("x");
            A a = new("a");
            var q = Select(x.Id, Select(a.Id).From(a).Union.Select(a.Id).From(a)
                .OrderBy({|#0:x.Id|}).Limit(1)).From(x);
            """, "postgresql");

    [Fact]
    public async Task QualifiedKeyAfterUnion_EveryQualifiedKey_ReportsEach()
    {
        var test = AnalyzerVerifier.Create(
            Usage("""
                A a = new("a");
                A b = new("b");
                var q = Select(a.Id, t.Id).From(a).CrossJoin(t).Union.Select(b.Id, s.Id).From(b)
                    .CrossJoin(s).OrderBy({|#0:a.Id|}, t.Id, 2, {|#1:b.Id.Desc|});
                """),
            AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(0));
        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("SQLA0102").WithLocation(1));
        await test.RunAsync();
    }

    // SQLite and SQL Server sort by a column every branch selects in its place (#792).
    [Theory]
    [InlineData("sqlite")]
    [InlineData("sqlserver")]
    public Task QualifiedKeyAfterUnion_SortingDialect_StaysSilent(string dbms) =>
        RunSilent("""
            A a = new("a");
            A b = new("b");
            var q = Select(a.Id).From(a).Union.Select(b.Id).From(b).OrderBy(a.Id);
            """, dbms);

    [Theory]
    // An unaliased table's column renders bare.
    [InlineData("""
        var q = Select(t.Id).From(t).Union.Select(s.Id).From(s).OrderBy(t.Id);
        """)]
    // A position and a first-branch alias are the docs' remedies.
    [InlineData("""
        A a = new("a");
        var n = a.Id.As("n");
        var q = Select(n).From(a).Union.Select(a.Id).From(a).OrderBy(1, n);
        """)]
    // INTERSECT, EXCEPT and MINUS have no twins yet, alone or beside a UNION.
    [InlineData("""
        A a = new("a");
        var q = Select(a.Id).From(a).Intersect.Select(a.Id).From(a).OrderBy(a.Id);
        """)]
    [InlineData("""
        A a = new("a");
        var q = Select(a.Id).From(a).Union.Select(a.Id).From(a).Except.Select(a.Id).From(a)
            .OrderBy(a.Id);
        """)]
    // No set operator: the query's own ORDER BY.
    [InlineData("""
        A a = new("a");
        var q = Select(a.Id).From(a).OrderBy(a.Id);
        """)]
    // A window's ordering is no compound's ORDER BY.
    [InlineData("""
        A a = new("a");
        var q = Select(a.Id, RowNumber().Over(OrderBy(a.Id))).From(a).Union
            .Select(a.Id, a.Id).From(a).OrderBy(1);
        """)]
    // An expression key, and keys passed as an array the caller built.
    [InlineData("""
        A a = new("a");
        object[] keys = [a.Id];
        var q = Select(a.Id).From(a).Union.Select(a.Id).From(a).OrderBy(a.Id + 1);
        var r = Select(a.Id).From(a).Union.Select(a.Id).From(a).OrderBy(keys);
        """)]
    // An alias the analyzer cannot prove: a variable, or a table assigned twice.
    [InlineData("""
        string alias = "a";
        A a = new(alias);
        var q = Select(a.Id).From(a).Union.Select(a.Id).From(a).OrderBy(a.Id);
        """)]
    [InlineData("""
        A a = new("a");
        a = new A();
        var q = Select(a.Id).From(a).Union.Select(a.Id).From(a).OrderBy(a.Id);
        """)]
    // The compound's head held in a variable leaves the operators before it unseen.
    [InlineData("""
        A a = new("a");
        var l = Select(a.Id).From(a);
        var q = l.Union.Select(a.Id).From(a).OrderBy(a.Id);
        """)]
    public Task QualifiedKeyAfterUnion_PostgreSql_StaysSilent(string statements) =>
        RunSilent(statements, "postgresql");

    [Theory]
    [InlineData("oracle", "Select(Distinct, t.Dep).From(t)")]
    [InlineData("postgresql", "Select(Distinct, t.Dep).From(t)")]
    [InlineData("postgresql", "Select(DistinctOn(t.Dep), t.Dep).From(t).OrderBy(t.Dep)")]
    [InlineData("oracle", "Select(Count(t.Id)).From(t)")]
    [InlineData("postgresql", "Select(Count(t.Id)).From(t)")]
    [InlineData("oracle", "Select(Coalesce(Max(t.Dep), 0)).From(t)")]
    [InlineData("postgresql", "Select(Sum(t.Id).Filter(t.Dep > 0).As(\"s\")).From(t)")]
    [InlineData("postgresql", "Select(t.Id, RowNumber().Over(OrderBy(t.Id))).From(t)")]
    [InlineData("postgresql", "Select(t.Id).From(t).OrderBy(RowNumber().Over(OrderBy(t.Id)))")]
    [InlineData("postgresql", "Select(Sum(t.Id).Over()).From(t)")]
    [InlineData("postgresql", "With(c.As(Select(t.Id).From(t))).Select(Distinct, t.Id).From(t)")]
    public Task ForUpdateOverDerivedRows_ReportsSqla0102(string dbms, string query) =>
        RunReporting($$"""
            Cte c = new("c");
            var q = {|#0:{{query}}.ForUpdate()|};
            """, dbms);

    // Oracle runs a lock beside a window function, and DistinctOn is oracle:false in the
    // matrix, which SQLA0100 reports.
    [Theory]
    [InlineData("Select(t.Id, RowNumber().Over(OrderBy(t.Id))).From(t)")]
    [InlineData("Select(Sum(t.Id).Over()).From(t)")]
    [InlineData("Select(t.Id).From(t).OrderBy(RowNumber().Over(OrderBy(t.Id)))")]
    public Task ForUpdateWithWindowFunction_Oracle_StaysSilent(string query) =>
        RunSilent($$"""
            var q = {{query}}.ForUpdate();
            """, "oracle");

    // The aggregate sits in a scalar subquery, a query block the lock does not reach.
    [Fact]
    public Task ForUpdateWithAggregateInScalarSubquery_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id, Select(Count(s.Id)).From(s).Where(s.Dep == t.Id))
                .From(t).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateWithAggregateViaVariable_PostgreSql_StaysSilent() =>
        RunSilent("""
            SqlExpression n = Count(t.Id);
            var q = Select(n).From(t).ForUpdate();
            """, "postgresql");

    [Fact]
    public Task ForUpdateWithAggregate_MySql_StaysSilent() =>
        RunSilent("""
            var q = Select(Distinct, Count(t.Id)).From(t).ForUpdate();
            """);

    // The GROUP BY rule reports the lock; this rule does not report it a second time.
    [Fact]
    public Task ForUpdateAfterGroupByWithAggregate_PostgreSql_ReportsOnce() =>
        RunReporting("""
            var q = {|#0:Select(Distinct, t.Dep, Count(t.Id)).From(t).GroupBy(t.Dep).OrderBy(t.Dep)
                .ForUpdate()|};
            """, "postgresql");

    [Fact]
    public Task ForUpdateAfterSetOperatorWithDistinct_PostgreSql_ReportsOnce() =>
        RunReporting("""
            var q = {|#0:Select(t.Id).From(t).Union.Select(Distinct, s.Id).From(s).ForUpdate()|};
            """, "postgresql");

    [Fact]
    public Task ForUpdateWithPlainSelect_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id, t.Dep + 1).From(t).OrderBy(t.Id).ForUpdate();
            """, "postgresql");

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

    // --- ORDER BY in a subquery on SQL Server (#742) ---

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(Exists({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var d = {|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}.AsTable("d");
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|})).Select(c.Column("dep")).From(c);
        """)]
    [InlineData("""
        var q = Select({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}).From(t);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id == {|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|});
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In({|#0:Select(Distinct, s.Dep).From(s).Where(s.Id > 1).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .CrossApply({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}, new DerivedTable("x"));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .OuterApply({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}, new DerivedTable("x"));
        """)]
    public Task OrderByInSubquery_SqlServer_ReportsSqla0102(string statements) =>
        RunReporting(statements, "sqlserver");

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(Select(Top(2), s.Dep).From(s).OrderBy(s.Dep)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(Distinct, Top(2), s.Dep).From(s).OrderBy(s.Dep)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep).OffsetRows(0)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep).OffsetRows(0).FetchNext(2)));
        """)]
    // The statement's own ORDER BY, and a window's.
    [InlineData("""
        var q = Select(t.Id).From(t).OrderBy(t.Id);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(RowNumber().Over(OrderBy(s.Dep))).From(s)));
        """)]
    // Held in a variable, the chain's embedding is out of the rule's sight.
    [InlineData("""
        var sorted = Select(s.Dep).From(s).OrderBy(s.Dep);
        var q = Select(t.Id).From(t).Where(t.Id.In(sorted));
        """)]
    public Task OrderByInSubquery_SqlServer_StaysSilent(string statements) =>
        RunSilent(statements, "sqlserver");

    // PostgreSQL 16 runs an ORDER BY in a subquery.
    [Fact]
    public Task OrderByInSubquery_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Select(t.Id).From(t).Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep)));
            """, "postgresql");

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.NotIn({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(Exists({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(NotExists({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id > Any({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id > All({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id > Some({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}));
        """)]
    [InlineData("""
        var q = Select({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}).From(t);
        """)]
    [InlineData("""
        var q = Select({|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}.As("m")).From(t);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id == {|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|});
        """)]
    [InlineData("""
        var q = Update(t).Set(t.Dep == {|#0:Select(s.Dep).From(s).OrderBy(s.Dep)|}).Where(t.Id == 1);
        """)]
    public Task OrderByInSubquery_Oracle_ReportsSqla0102(string statements) =>
        RunReporting(statements, "oracle");

    // Oracle XE 21.3.0 and Free 23ai run it in a derived table and a CTE body; a lateral
    // source is unmeasured there.
    [Theory]
    [InlineData("""
        var d = Select(s.Dep).From(s).OrderBy(s.Dep).AsTable("d");
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As(Select(s.Dep).From(s).OrderBy(s.Dep))).Select(c.Column("dep")).From(c);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .CrossApply(Select(s.Dep).From(s).OrderBy(s.Dep), new DerivedTable("x"));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep).FetchFirst(2)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t)
            .Where(t.Id.In(Select(s.Dep).From(s).OrderBy(s.Dep).OffsetRows(0)));
        """)]
    [InlineData("""
        var sorted = Select(s.Dep).From(s).OrderBy(s.Dep);
        var q = Select(t.Id).From(t).Where(t.Id.In(sorted));
        """)]
    // A helper sharing a predicate host's name may embed the query anywhere.
    [InlineData("""
        ISqlBuilder In(ISubquery sub) => Select(Asterisk).From(sub.AsTable("d"));
        var q = In(Select(s.Dep).From(s).OrderBy(s.Dep));
        """)]
    public Task OrderByInSubquery_Oracle_StaysSilent(string statements) =>
        RunSilent(statements, "oracle");

    // --- An ordered, row-limited query as a MERGE INSERT value on Oracle (#828) ---

    private const string MergeHead = """
        A u = new("u");
        A v = new("v");
        var m = MergeInto(u).Using(v).On(u.Id == v.Id)
        """;

    [Theory]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values({|#0:Select(s.Id).From(s).OrderBy(s.Id)|}.OffsetRows(0).FetchNext(1));
        """)]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values({|#0:Select(s.Id).From(s).OrderBy(s.Id)|}.OffsetRows(1));
        """)]
    [InlineData("""
        .WhenNotMatched().ThenInsert()
            .Values(v.Id, {|#0:Select(s.Id).From(s).Where(s.Id == v.Id).OrderBy(s.Id)|}
                .OffsetRows(0).FetchNext(1));
        """)]
    public Task OrderedOffsetInMergeInsertValue_Oracle_ReportsSqla0102(string branch) =>
        Task.WhenAll(
            RunReporting(MergeHead + branch, "oracle"),
            RunAsync(
                Usage(MergeHead + branch),
                AnalyzerVerifier.EditorConfig("oracle", "23"),
                expectWarning: true));

    // Oracle has no WHEN NOT MATCHED AND, so the conditioned branch reports only once that
    // construct is declared supported.
    [Fact]
    public Task OrderedOffsetInConditionedMergeInsertValue_Oracle_ReportsSqla0102() =>
        RunAsync(
            Usage(MergeHead + """
                .WhenNotMatched(v.Id > 0).ThenInsert(u.Id)
                    .Values({|#0:Select(s.Id).From(s).OrderBy(s.Id)|}.OffsetRows(1));
                """),
            AnalyzerVerifier.EditorConfig("oracle") + """

                sqlartisan_construct_when_not_matched_arity1 = supported
                """,
            expectWarning: true);

    // Free 23ai runs the ordered FETCH FIRST form that XE 21.3.0 rejects.
    [Fact]
    public Task OrderedFetchFirstInMergeInsertValue_Oracle_ReportsBelowTheFloorOnly() =>
        Task.WhenAll(
            RunReporting(
                MergeHead + """
                    .WhenNotMatched().ThenInsert(u.Id)
                        .Values({|#0:Select(s.Id).From(s).OrderBy(s.Id)|}.FetchFirst(1));
                    """,
                "oracle"),
            RunAsync(
                AnalyzerVerifier.Unmarked(Usage(MergeHead + """
                    .WhenNotMatched().ThenInsert(u.Id)
                        .Values(Select(s.Id).From(s).OrderBy(s.Id).FetchFirst(1));
                    """)),
                AnalyzerVerifier.EditorConfig("oracle", "23"),
                expectWarning: false));

    // XE 21.3.0 and Free 23ai run every one: unordered, nested in an expression, in UPDATE
    // SET, in a plain INSERT; a held query, a compound or a wrapped one is out of sight.
    [Theory]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values(Select(s.Id).From(s).OffsetRows(0).FetchNext(1));
        """)]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values(Coalesce(Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(0).FetchNext(1), 0));
        """)]
    [InlineData("""
        .WhenMatched()
            .ThenUpdateSet(u.Id == Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(0).FetchNext(1));
        """)]
    [InlineData("""
        ;
        var i = InsertInto(t, t.Id).Values(Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(1));
        """)]
    [InlineData("""
        ;
        var sub = Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(1);
        var n = m.WhenNotMatched().ThenInsert(u.Id).Values(sub);
        """)]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values(Select(s.Id).From(s).Union.Select(t.Id).From(t).OrderBy(1).OffsetRows(1));
        """)]
    [InlineData("""
        .WhenNotMatched().ThenInsert(u.Id)
            .Values(Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(1).As("x"));
        """)]
    public Task RowLimitedQueryElsewhereInMerge_Oracle_StaysSilent(string branch) =>
        RunSilent(MergeHead + branch, "oracle");

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    public Task OrderedOffsetInMergeInsertValue_OffOracle_StaysSilent(string dbms) =>
        RunSilent(
            MergeHead + """
                .WhenNotMatched().ThenInsert(u.Id)
                    .Values(Select(s.Id).From(s).OrderBy(s.Id).OffsetRows(0).FetchNext(1));
                """,
            dbms);

    // --- A compound's ORDER BY in a subquery on SQL Server (#822) ---

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.NotIn(
            {|#0:Select(t.Id).From(t).UnionAll.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(Exists(
            {|#0:Select(t.Id).From(t).Intersect.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id > Any(
            {|#0:Select(t.Id).From(t).Except.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2)));
        """)]
    [InlineData("""
        var d = {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
            .OffsetRows(1).FetchNext(2).AsTable("d");
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As({|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
            .OffsetRows(1).FetchNext(2))).Select(c.Column("id")).From(c);
        """)]
    [InlineData("""
        var q = Select({|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
            .OffsetRows(1).FetchNext(1)).From(t);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id == {|#0:Select(t.Id).From(t).Union.Select(s.Dep)
            .From(s).OrderBy(1)|}.OffsetRows(1).FetchNext(1));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).CrossApply(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2), new DerivedTable("x"));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).OuterApply(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2), new DerivedTable("x"));
        """)]
    // An offset alone pages too, and so does a FETCH after a zero offset.
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}.OffsetRows(1)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}
                .OffsetRows(0).FetchNext(2)));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In({|#0:Select(t.Id).From(t).Where(t.Id > 1)
            .Union.Select(s.Dep).From(s).Except.Select(s.Id).From(s).OrderBy(1)|}
                .OffsetRows(1).FetchNext(2)));
        """)]
    public Task PagedCompoundInSubquery_SqlServer_ReportsSqla0102(string statements) =>
        RunReporting(statements, "sqlserver");

    [Theory]
    // At the top level the paging takes the combined result.
    [InlineData("""
        var q = Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)
            .OffsetRows(1).FetchNext(2);
        """)]
    // The workaround: wrap the compound first, then sort and page outside it.
    [InlineData("""
        Cte w = new("w");
        var q = With(w.As(Select(t.Id).From(t).Union.Select(s.Dep).From(s)))
            .Select(w.Column("id")).From(w).OrderBy(w.Column("id")).OffsetRows(1).FetchNext(2);
        """)]
    [InlineData("""
        var d = Select(t.Id).From(t).Union.Select(s.Dep).From(s).AsTable("d");
        var q = Select(t.Id).From(t).Where(t.Id.In(
            Select(d.Column("id")).From(d).OrderBy(d.Column("id")).OffsetRows(1).FetchNext(2)));
        """)]
    // The compound sits in an operand of the paged query, not in its own chain.
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(Select(s.Id).From(s)
            .Where(s.Id.In(Select(t.Id).From(t).Union.Select(s.Dep).From(s)))
            .OrderBy(s.Id).OffsetRows(1).FetchNext(2)));
        """)]
    // A zero offset alone skips nothing, wherever it binds.
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1).OffsetRows(0)));
        """)]
    // TOP in a branch beside the compound's paging is unmeasured.
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(Select(Top(3), t.Id).From(t)
            .Union.Select(s.Dep).From(s).OrderBy(1).OffsetRows(1).FetchNext(2)));
        """)]
    // Held in a variable, or with its first branch held, the chain is out of sight.
    [InlineData("""
        var page = Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)
            .OffsetRows(1).FetchNext(2);
        var q = Select(t.Id).From(t).Where(t.Id.In(page));
        """)]
    [InlineData("""
        var first = Select(t.Id).From(t);
        var q = Select(t.Id).From(t).Where(t.Id.In(
            first.Union.Select(s.Dep).From(s).OrderBy(1).OffsetRows(1).FetchNext(2)));
        """)]
    public Task PagedCompoundInSubquery_SqlServer_StaysSilent(string statements) =>
        RunSilent(statements, "sqlserver");

    // The other engines page the combined result in every position measured (#823).
    [Theory]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    public Task PagedCompoundInSubquery_OtherDbms_StaysSilent(string dbms) =>
        RunSilent("""
            Cte c = new("c");
            var q = With(c.As(Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)
                .OffsetRows(1).FetchNext(2))).Select(c.Column("id")).From(c);
            """, dbms);

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}));
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(Exists(
            {|#0:Select(t.Id).From(t).Except.Select(s.Dep).From(s).OrderBy(1)|}));
        """)]
    [InlineData("""
        var d = {|#0:Select(t.Id).From(t).UnionAll.Select(s.Dep).From(s).OrderBy(1)|}.AsTable("d");
        """)]
    [InlineData("""
        Cte c = new("c");
        var q = With(c.As({|#0:Select(t.Id).From(t).Intersect.Select(s.Dep).From(s).OrderBy(1)|}))
            .Select(c.Column("id")).From(c);
        """)]
    [InlineData("""
        var q = Select({|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|}).From(t);
        """)]
    [InlineData("""
        var q = Select(t.Id).From(t).CrossApply(
            {|#0:Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)|},
            new DerivedTable("x"));
        """)]
    public Task OrderedCompoundInSubquery_SqlServer_ReportsSqla0102(string statements) =>
        RunReporting(statements, "sqlserver");

    [Theory]
    [InlineData("""
        var q = Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1);
        """, "sqlserver")]
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(Select(Top(3), t.Id).From(t)
            .Union.Select(s.Dep).From(s).OrderBy(1)));
        """, "sqlserver")]
    // Oracle's verdict on an ordered compound is unmeasured.
    [InlineData("""
        var q = Select(t.Id).From(t).Where(t.Id.In(
            Select(t.Id).From(t).Union.Select(s.Dep).From(s).OrderBy(1)));
        """, "oracle")]
    public Task OrderedCompoundInSubquery_StaysSilent(string statements, string dbms) =>
        RunSilent(statements, dbms);
}
