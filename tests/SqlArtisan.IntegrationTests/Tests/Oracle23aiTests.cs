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

    // #521 probe: Oracle's filtered MERGE branch — the filter is a trailing
    // WHERE on the action, and WHEN [NOT] MATCHED takes no AND.
    [Fact]
    public void MergeFilteredBranch_TakesTrailingWhereNotAnd()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name FROM dual) s "
                + "ON (t.id = s.id) WHEN MATCHED THEN UPDATE SET t.name = s.name "
                + "WHERE s.name = 'x'",
            transaction: transaction);

        connection.Execute(
            "MERGE INTO users t USING (SELECT 901 AS id, 'x' AS name FROM dual) s "
                + "ON (t.id = s.id) WHEN NOT MATCHED THEN INSERT (id, name) "
                + "VALUES (s.id, s.name) WHERE s.name = 'x'",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name FROM dual) s "
                    + "ON (t.id = s.id) WHEN MATCHED AND s.name = 'x' THEN "
                    + "UPDATE SET t.name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // #521 probe: does Oracle's FOR UPDATE OF take a column list, and does it
    // take a bare table name the way PostgreSQL's does?
    [Fact]
    public void ForUpdateOf_TakesAColumnListNotATable()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.ExecuteScalar(
            "SELECT t.id FROM users t WHERE t.id = 1 FOR UPDATE OF t.id",
            transaction: transaction);
        connection.ExecuteScalar(
            "SELECT t.id FROM users t WHERE t.id = 1 FOR UPDATE OF t.id, t.name",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar(
                "SELECT t.id FROM users t WHERE t.id = 1 FOR UPDATE OF t",
                transaction: transaction));
        transaction.Rollback();
    }
}
