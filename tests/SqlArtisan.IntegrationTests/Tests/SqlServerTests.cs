using System.Data;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "SqlServer")]
public sealed class SqlServerTests : IntegrationTestBase, IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerTests(SqlServerFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public void Pagination_OffsetFetch_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));

        Assert.Equal(new[] { 2, 3 }, ids);
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

    [Fact] // Regression for #168: STRING_AGG's separator is now emitted as an
           // inline literal, which SQL Server requires (it rejects a parameter).
    public void StringAggregation_StringAgg_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(StringAgg(u.Name, ",")).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void SetOperator_Except_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Users {1..5} EXCEPT the users referenced by orders {1,2,3,5} = {4}.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Except.Select(o.UserId).From(o))
            .Single();

        Assert.Equal(4, id);
    }
}
