using System.Text;
using SqlArtisan.IntegrationTests.Randomized;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Temporary #828 probe: records which row-limited subquery positions each engine takes.
internal static class Probe828
{
    public static void Run(Func<PlannedStatement, string?> rejection, Dbms dbms)
    {
        RandomDeptsTable d2 = new("d2");
        RandomUsersTable u = new("u");
        RandomUsersTable s = new("s");
        RandomUsersTable t = new("");
        List<(string Name, PlannedStatement Statement)> cases = [];

        void Value(string name, Func<SqlExpression, object> value) =>
            cases.Add((name, UpsertMergeTwins.MergeInsertValue(name, value)));

        Value("C0 values ordered fetchFirst", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).OrderBy(d2.Id).FetchFirst(1));
        Value("C1 values ordered offset+fetchNext", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).OrderBy(d2.Id).OffsetRows(0).FetchNext(1));
        Value("M1a values unordered fetchFirst", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).FetchFirst(1));
        Value("M1b values unordered offset+fetchNext", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).OffsetRows(0).FetchNext(1));
        Value("M2a values ordered offset only", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).OrderBy(d2.Id).OffsetRows(0));
        Value("M2b values unordered offset only", a =>
            Select(d2.Id).From(d2).Where(d2.Id == a).OffsetRows(0));
        Value("M3a values coalesce(ordered fetchFirst)", a => Coalesce(
            Select(d2.Id).From(d2).Where(d2.Id == a).OrderBy(d2.Id).FetchFirst(1), 0));
        Value("M3b values coalesce(ordered offset+fetchNext)", a => Coalesce(
            Select(d2.Id).From(d2).Where(d2.Id == a).OrderBy(d2.Id).OffsetRows(0).FetchNext(1),
            0));
        Value("M3c values uncorrelated ordered fetchFirst", a =>
            Select(d2.Id).From(d2).OrderBy(d2.Id).FetchFirst(1));
        Value("M3d values aggregate", a => Select(Max(d2.Id)).From(d2).Where(d2.Id == a));

        cases.Add(("M4a plain insert ordered fetchFirst", UpsertMergeTwins.Statement(
            "M4a",
            () => InsertInto(t, t.Id, t.Age)
                .Values(700, Select(d2.Id).From(d2).OrderBy(d2.Id).FetchFirst(1)))));
        cases.Add(("M4b plain insert ordered offset+fetchNext", UpsertMergeTwins.Statement(
            "M4b",
            () => InsertInto(t, t.Id, t.Age)
                .Values(700, Select(d2.Id).From(d2).OrderBy(d2.Id).OffsetRows(0).FetchNext(1)))));
        cases.Add(("M5a update set ordered fetchFirst", UpsertMergeTwins.Statement(
            "M5a",
            () => MergeInto(u).Using(s).On(u.Id == s.Id).WhenMatched()
                .ThenUpdateSet(u.Age == Select(d2.Id).From(d2).Where(d2.Id == s.Age)
                    .OrderBy(d2.Id).FetchFirst(1)))));
        cases.Add(("M5b update set ordered offset+fetchNext", UpsertMergeTwins.Statement(
            "M5b",
            () => MergeInto(u).Using(s).On(u.Id == s.Id).WhenMatched()
                .ThenUpdateSet(u.Age == Select(d2.Id).From(d2).Where(d2.Id == s.Age)
                    .OrderBy(d2.Id).OffsetRows(0).FetchNext(1)))));

        ExpressionAlias v1 = Select(d2.Id).From(d2).Where(d2.Id == s.Age)
            .OrderBy(d2.Id).OffsetRows(0).FetchNext(1).As("v");
        SubqueryDerivedTable src1 = Select(s.Id, v1).From(s).AsTable("src");
        cases.Add(("M6a source derived offset+fetchNext", UpsertMergeTwins.Statement(
            "M6a",
            () => MergeInto(u).Using(src1).On(u.Id == src1.Column("id") + 10)
                .WhenNotMatched().ThenInsert(u.Id, u.Age)
                .Values(src1.Column("id") + 10, src1.Column(v1)))));
        ExpressionAlias v2 = Select(d2.Id).From(d2).Where(d2.Id == s.Age)
            .OrderBy(d2.Id).FetchFirst(1).As("v");
        SubqueryDerivedTable src2 = Select(s.Id, v2).From(s).AsTable("src");
        cases.Add(("M6b source derived fetchFirst", UpsertMergeTwins.Statement(
            "M6b",
            () => MergeInto(u).Using(src2).On(u.Id == src2.Column("id") + 10)
                .WhenNotMatched().ThenInsert(u.Id, u.Age)
                .Values(src2.Column("id") + 10, src2.Column(v2)))));

        StringBuilder report = new($"PROBE828 {dbms}\n");
        foreach ((string name, PlannedStatement statement) in cases)
        {
            string sql;
            try
            {
                sql = statement.Builder().Build(dbms).Text;
            }
            catch (Exception ex)
            {
                report.Append($"  {name}: BUILD {ex.GetType().Name}: {ex.Message}\n");
                continue;
            }

            if (dbms == Dbms.SqlServer && name.Contains("fetchFirst", StringComparison.Ordinal))
            {
                report.Append($"  {name}: SKIP\n");
                continue;
            }

            string? error = rejection(statement);
            report.Append($"  {name}: {(error is null ? "RUNS" : "REJECTED " + error)}\n");
            report.Append($"      {sql}\n");
        }

        Assert.Fail(report.ToString());
    }
}

public sealed partial class OracleCombinatorialDmlTests
{
    [Fact]
    public void Probe828_RowLimitedSubqueryPositions() => Probe828.Run(Rejection, Dbms.Oracle);
}

public sealed partial class Oracle23aiCombinatorialDmlTests
{
    [Fact]
    public void Probe828_RowLimitedSubqueryPositions() => Probe828.Run(Rejection, Dbms.Oracle);
}

public sealed partial class PostgreSqlCombinatorialDmlTests
{
    [Fact]
    public void Probe828_RowLimitedSubqueryPositions() =>
        Probe828.Run(Rejection, Dbms.PostgreSql);
}

public sealed partial class SqlServerCombinatorialDmlTests
{
    [Fact]
    public void Probe828_RowLimitedSubqueryPositions() =>
        Probe828.Run(Rejection, Dbms.SqlServer);
}
