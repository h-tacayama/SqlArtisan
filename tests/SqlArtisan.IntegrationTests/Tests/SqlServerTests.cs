using System.Data;
using Dapper;
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
    public void Sequence_NextValueFor_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long next = Convert.ToInt64(connection.ExecuteScalar(
            Select(NextValueFor("test_seq")).From(u).Where(u.Id == 1)));

        Assert.True(next >= 1);
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
    public void Merge_WhenNotMatchedBySource_DeletesUnmatched()
    {
        UsersTable t = new("t");
        OrdersTable o = new("o");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // MERGE users with orders on id = user_id: target users referenced by no
        // order (only Dave, id 4) are NOT MATCHED BY SOURCE and get deleted,
        // leaving {1, 2, 3, 5}.
        connection.Execute(
            MergeInto(t)
                .Using(o)
                .On(t.Id == o.UserId)
                .WhenNotMatchedBySource().ThenDelete(),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(4, count);
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

    [Fact]
    public void JsonValue_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_VALUE(data, '$.name') extracts a scalar from the NVARCHAR(MAX) column.
        string name = connection
            .Query<string>(Select(JsonValue(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonQuery_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_QUERY(data, '$.address') returns the nested JSON object.
        string address = connection
            .Query<string>(Select(JsonQuery(u.Data, "$.address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact] // #254: T-SQL cannot alias the target of an INSERT/UPDATE/DELETE directly
           // (the alias must come from a FROM clause — the joined-DML form), so the
           // text SqlArtisan would emit for an aliased target, `UPDATE users AS "cu"
           // ...`, is a syntax error here. This anchors the Build(SqlServer) guard in
           // InsertBuilder/UpdateBuilder/DeleteBuilder: with no valid spelling to
           // emit, it throws rather than produce SQL the engine rejects. Raw SQL by
           // necessity — the guard would otherwise throw before the statement ever
           // reached the DB.
    public void AliasedDmlTarget_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The unaliased forms are valid T-SQL (so the table and columns are right),
        // run inside a rolled-back transaction to leave the seed untouched.
        using (IDbTransaction tx = connection.BeginTransaction())
        {
            connection.Execute("INSERT INTO users (id, name) VALUES (999, 'x')", transaction: tx);
            connection.Execute("UPDATE users SET name = name WHERE id = 1", transaction: tx);
            connection.Execute("DELETE FROM users WHERE id = -1", transaction: tx);
            tx.Rollback();
        }

        // The only difference — aliasing the target — is what SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "INSERT INTO users AS \"cu\" (id, name) VALUES (999, 'x')"));
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "UPDATE users AS \"cu\" SET name = 'x' WHERE \"cu\".id = 1"));
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "DELETE FROM users AS \"cu\" WHERE \"cu\".id = 1"));
    }
}
