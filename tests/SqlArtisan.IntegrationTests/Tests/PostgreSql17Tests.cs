using System.Data;
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
}
