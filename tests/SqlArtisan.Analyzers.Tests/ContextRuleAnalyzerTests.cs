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

    // A join reached through From(...) is the FROM-form's own join, not the
    // direct-join spelling the other rule reports.
    [Fact]
    public Task JoinedUpdateFromJoin_PostgreSql_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Id == s.Id).From(s).InnerJoin(s).On(t.Dep == s.Id);
            """, "postgresql");

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
}
