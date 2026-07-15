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

    [Fact] // #241 (GAP-19): SQL Server matches GROUP BY expressions syntactically,
           // so a parameterized SELECT expression repeated with fresh markers fails
           // with Msg 8120 (live-verified). Raw SQL by necessity — SqlArtisan now
           // reuses a shared instance's markers and cannot emit this form.
    public void GroupByBindMarkerMismatch_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The expression itself is valid (table and columns are right).
        connection.Execute(
            "SELECT CASE department_id WHEN @p0 THEN @p1 ELSE @p2 END FROM users",
            new { p0 = 10, p1 = "Low", p2 = "Other" });

        // The only difference — distinct markers in GROUP BY — is what SS rejects.
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "SELECT CASE department_id WHEN @p0 THEN @p1 ELSE @p2 END FROM users "
                + "GROUP BY CASE department_id WHEN @p3 THEN @p4 ELSE @p5 END",
            new { p0 = 10, p1 = "Low", p2 = "Other", p3 = 10, p4 = "Low", p5 = "Other" }));
    }

    [Fact] // ADR 0012 (#295): anchors the "no engine accepts it" premise — raw
           // SQL by necessity, since PercentileFractionGuard now rejects this client-side.
    public void PercentileCont_FractionOutOfRange_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The in-range form is valid (so the table and column are right).
        // SQL Server requires the windowed OVER() form (matching MatrixSweepCatalog).
        connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) OVER () FROM users");

        // The only difference — an out-of-range fraction — is what SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(1.5) WITHIN GROUP (ORDER BY age) OVER () FROM users"));
    }

    [Fact]
    public void JoinedUpdateFrom_Executes()
    {
        // The target alias comes from FROM, the T-SQL spelling that finally makes
        // an aliased UPDATE target valid on SQL Server.
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).Set(u.Age == 999).From(u).InnerJoin(o).On(u.Id == o.UserId).Where(u.Id == 3),
            transaction);

        int age = connection
            .Query<int>(Select(read.Age).From(read).Where(read.Id == 3), transaction)
            .Single();

        Assert.Equal(999, age);
        transaction.Rollback();
    }

    [Fact]
    public void JoinedDeleteFrom_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            DeleteFrom(u).From(u).InnerJoin(o).On(u.Id == o.UserId).Where(u.Id == 3),
            transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(read.Id)).From(read).Where(read.Id == 3), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }
}
