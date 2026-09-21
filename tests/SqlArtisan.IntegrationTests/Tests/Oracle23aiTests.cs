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
}
