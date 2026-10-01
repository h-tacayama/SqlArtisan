using System.Data;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine facts true at Oracle 23ai but not at the 21c baseline lane. Each
/// test is the live gate for a doc claim whose rejecting half the 21c lane
/// (<see cref="OracleTests"/>) asserts.
/// </summary>
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public Oracle23aiTests(Oracle23aiFixture fixture)
    {
        _fixture = fixture;
    }

    // The accepting half of the query-statements doc note: 23ai added the
    // multi-row VALUES table value constructor.
    [Fact]
    public void MultiRowValues_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name).Values(201, "A").Values(202, "B"),
            transaction);

        long inserted = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id.In(201, 202)), transaction));

        Assert.Equal(2, inserted);
        transaction.Rollback();
    }

    // The 23ai half of LockWaitGuard's live-verified claim (ADR 0012); the 21c
    // half is OracleTests.Wait_NegativeSeconds_Rejected.
    [Fact]
    public void Wait_NegativeSeconds_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 3");
        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 0");

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT -1"));
    }

    // The 21c lane's leading-WITH-before-MERGE rejection, re-run at 23ai: the
    // subquery-factoring clause is still no part of the MERGE grammar.
    [Fact]
    public void LeadingWithBeforeMerge_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name FROM dual) s "
                + "ON (t.id = s.id) WHEN MATCHED THEN UPDATE SET t.name = s.name",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "WITH c AS (SELECT 1 AS id, 'x' AS name FROM dual) "
                    + "MERGE INTO users t USING c s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // #590: Oracle Free 23ai takes FOR UPDATE only in a top-level SELECT; each unlocked control
    // runs, so the lock is what the engine rejects.
    [Fact]
    public void LockedSubqueryInIn_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedExistsSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id = (SELECT user_id FROM orders WHERE id = 1)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id = "
            + "(SELECT user_id FROM orders WHERE id = 1 FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedSelectedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id, (SELECT o.id FROM users o WHERE o.id = u.id) FROM users u").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT u.id, (SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) "
            + "FROM users u").ToList());
    }

    [Fact]
    public void LockedCrossApply_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id FROM users u "
            + "CROSS APPLY (SELECT o.id FROM users o WHERE o.id = u.id) x").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT u.id FROM users u "
            + "CROSS APPLY (SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    [Fact]
    public void LockedLateralInlineView_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id) x").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    [Fact]
    public void LockedCteBody_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "WITH c AS (SELECT id FROM users) SELECT id FROM c").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "WITH c AS (SELECT id FROM users FOR UPDATE) SELECT id FROM c").ToList());
    }

    [Fact]
    public void LockedDerivedTable_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users) d").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users FOR UPDATE) d").ToList());
    }

    // #582: the acceptance twin behind SQLA0102's Oracle 23 floor for the FROM form,
    // as SqlArtisan emits it. orders.id is unique, so no row is updated twice.
    [Fact]
    public void JoinedUpdateFrom_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).Set(u.Age == 999).From(o).Where(u.Id == o.Id),
            transaction);
        transaction.Rollback();
    }

    // #582: 23ai added UPDATE ... FROM, but the joined forms SqlArtisan withholds
    // RETURNING from stay rejected; the FROM control, led by the table name, runs.
    // Its source is one row per user: ORA-30926 rejects a row updated twice.
    [Fact]
    public void JoinedUpdateRelistedTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "UPDATE users \"u\" SET age = 1 FROM (SELECT DISTINCT user_id FROM orders) \"o\" "
                + "WHERE \"o\".user_id = \"u\".id",
            transaction: transaction);
        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "UPDATE \"u\" SET age = 1 FROM users \"u\" "
                    + "INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void JoinedDeleteLead_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "DELETE \"u\" FROM users \"u\" "
                    + "INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void JoinedUpdateJoinForm_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "UPDATE users \"u\" INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id "
                    + "SET \"u\".age = 1",
                transaction: transaction));
        transaction.Rollback();
    }
}
