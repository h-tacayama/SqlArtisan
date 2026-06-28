using System.Data;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "Oracle")]
public sealed class OracleTests : IntegrationTestBase, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleTests(OracleFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public void Pagination_OffsetFetch_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact(Skip = "Known bug #165: a re-aliased CTE column is alias-quoted at its "
        + "definition but referenced unquoted, so Oracle folds the reference to "
        + "uppercase and raises ORA-00904. Un-skip when #165 is fixed.")]
    public void Cte_AliasedColumn_KnownOracleBug()
    {
        UsersTable u = new();
        Cte seniors = new("seniors");
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            With(seniors.As(Select(u.Id.As(seniors.Column("id"))).From(u).Where(u.Age >= 40)))
                .Select(Count(seniors.Column("id")))
                .From(seniors)));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Merge_UpsertViaMerge_Executes()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t)
                .Using(s)
                .On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name)
                .WhenNotMatched().ThenInsert(c.Id, c.Name).Values(s.Id, s.Name),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(5, count);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_Listagg_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(Listagg(u.Name, ",").WithinGroup(OrderBy(u.Name))).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void SetOperator_Minus_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Oracle spells EXCEPT as MINUS: users {1..5} MINUS {1,2,3,5} = {4}.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Minus.Select(o.UserId).From(o))
            .Single();

        Assert.Equal(4, id);
    }
}
