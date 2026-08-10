using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "MySql")]
public sealed class MySqlTests : IntegrationTestBase, IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlTests(MySqlFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public void Pagination_LimitOffset_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).Limit(2).Offset(1));

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact]
    public void Upsert_OnDuplicateKeyUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Values(1, "AliceUpdated")
                .OnDuplicateKeyUpdate(u.Name == Excluded(u.Name)),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("AliceUpdated", name);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_GroupConcat_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name)).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void StringAggregation_GroupConcatSeparator_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // GROUP_CONCAT(name SEPARATOR ' | ') — the SEPARATOR clause is inlined.
        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name, Separator(" | "))).From(u))
            .Single();

        Assert.Contains(" | ", concatenated);
    }

    [Fact]
    public void JsonExtract_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_EXTRACT(data, '$.name') — the path is inlined as a literal. MySQL
        // returns the scalar as a quoted JSON string (e.g. "Alice"), so the value
        // is asserted with Contains rather than an exact match.
        string name = connection
            .Query<string>(Select(JsonExtract(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("Alice", name);
    }

    [Fact]
    public void JsonArrowText_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data ->> '$.name') — MySQL accepts a bound parameter as the path, so
        // the key binds normally; ->> returns the unquoted scalar.
        string name = connection
            .Query<string>(Select(JsonArrowText(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonArrow_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data -> '$.address') returns the nested JSON object.
        string address = connection
            .Query<string>(Select(JsonArrow(u.Data, "$.address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact] // #255 / #239 (ERG-09): MySQL accepts a single-table DELETE with an
           // aliased target (`DELETE FROM users AS `cu``) as of 8.0.16; the pinned
           // mysql:8.0 image is well past that boundary. This is the safe spelling
           // for a correlated DELETE on MySQL, so proving it runs clears the
           // grammar-unverified register entry.
    public void DeleteAliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(300, "Temp", 20, 99),
            transaction);
        connection.Execute(DeleteFrom(cu).Where(cu.Id == 300), transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == 300), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }

    [Fact]
    public void JoinedUpdateJoin_Executes()
    {
        // MySQL's multi-table UPDATE joins before SET.
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).InnerJoin(o).On(u.Id == o.UserId).Set(u.Age == 999).Where(u.Id == 3),
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

    [Fact] // #264: SQLA0102's live proof. Top-level LIMIT acceptance is proven by
           // Pagination_LimitOffset_Executes; the position is the only difference.
    public void ContextRule_LimitInInSubquery_Rejected()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u)
                .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)))));
    }

    [Fact] // SQLA0102 fires on NotIn too; MySQL's LIMIT restriction covers NOT IN
           // as well as IN/ALL/ANY/SOME, so the NOT IN arm is live-proven here.
    public void ContextRule_LimitInNotInSubquery_Rejected()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u)
                .Where(u.Id.NotIn(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)))));
    }

    [Fact] // #264: SQLA0102's live proof. GROUPING() under WITH ROLLUP is proven by
           // the dialect sweep's MySQL branch; the missing suffix is the only difference.
    public void ContextRule_GroupingWithoutWithRollup_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(Grouping(u.DepartmentId)).From(u).GroupBy(u.DepartmentId)));
    }

    [Fact] // #436: SQLA0102's live proof for the Interval context rule. Positioned
           // correctly (as a +/- operand) it is proven by the dialect sweep's MySQL
           // branch; the missing arithmetic wrapper is the only difference.
    public void ContextRule_IntervalOutsideArithmetic_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(Interval(30, DateTimePart.Day)).From(u)));
    }

    // #362: SQLA0205's live proof, and the reason it is a Warning rather than a
    // performance note. MySQL compares a string to a number as floating point, so
    // the mismatch changes which rows come back — not merely how fast.
    [Fact]
    public void TextColumnComparedToNumber_MatchesRowsThatAreNotEqualAsText()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute("CREATE TEMPORARY TABLE zip_probe (code varchar(16))");

        try
        {
            connection.Execute(
                "INSERT INTO zip_probe (code) VALUES ('150'), ('0150'), ('150abc'), ('abc')");

            IEnumerable<string> asNumber = connection.Query<string>(
                "SELECT code FROM zip_probe WHERE code = 150 ORDER BY code");

            IEnumerable<string> asText = connection.Query<string>(
                "SELECT code FROM zip_probe WHERE code = '150' ORDER BY code");

            Assert.Equal(new[] { "0150", "150", "150abc" }, asNumber);
            Assert.Equal(new[] { "150" }, asText);
        }
        finally
        {
            connection.Execute("DROP TEMPORARY TABLE IF EXISTS zip_probe");
        }
    }

    [Fact] // ADR 0012 (#402): anchors the "no engine accepts it" premise for the
           // NTILE/NTH_VALUE/window-frame guards — raw SQL by necessity, since
           // WindowFrameGuard now rejects these client-side.
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar("SELECT NTH_VALUE(age, 1) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 PRECEDING) FROM users");

        // The only difference each time — the value-domain violation — is what MySQL rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT NTILE(0) OVER (ORDER BY age) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT NTH_VALUE(age, 0) OVER (ORDER BY age) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS -1 PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS 1 FOLLOWING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN CURRENT ROW AND 1 PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED FOLLOWING AND UNBOUNDED FOLLOWING) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors the MySqlTemporalUnits list in
           // DatepartValidity.cs (shared by Extract and Interval) — EPOCH is a
           // PostgreSQL-only field, not a MySQL EXTRACT()/DATE_ADD() unit.
    public void Extract_EpochUnit_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed unit is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT EXTRACT(DAY FROM created_at) FROM users");

        // The only difference — the unit EXTRACT() doesn't have on MySQL — is
        // what MySQL rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT EXTRACT(EPOCH FROM created_at) FROM users"));
    }
}
