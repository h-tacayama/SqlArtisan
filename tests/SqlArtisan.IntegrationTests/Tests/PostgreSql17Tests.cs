using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine facts true at PostgreSQL 17 but not at the 16 baseline lane, each the live gate
/// for a record whose rejecting half the 16 lane (<see cref="PostgreSqlTests"/>) asserts.
/// </summary>
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17Tests : IClassFixture<PostgreSql17Fixture>
{
    private readonly PostgreSql17Fixture _fixture;

    public PostgreSql17Tests(PostgreSql17Fixture fixture)
    {
        _fixture = fixture;
    }

    // #582: public-api-design.md lists MERGE ... RETURNING (PostgreSQL 17) as not yet
    // offered; this is the engine side of that record.
    [Fact]
    public void MergeReturning_Executes()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users AS t USING users AS s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = s.name RETURNING t.id",
            transaction: transaction);
        transaction.Rollback();
    }

    // SQLA0102's twin (#582): WHEN NOT MATCHED BY SOURCE takes the 16 lane's rule for
    // the other two clauses — no branch after an unconditioned one of its clause.
    [Fact]
    public void MergeRepeatedBySourceBranch_NeedsAConditionOnTheEarlierBranch()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN DELETE "
                + "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET name = t.name",
            transaction: transaction);

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET name = t.name "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN DELETE",
            transaction: transaction));
        transaction.Rollback();
    }
}
