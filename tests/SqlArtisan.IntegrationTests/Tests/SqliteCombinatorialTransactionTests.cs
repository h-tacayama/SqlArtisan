using System.Data;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Randomized;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "Sqlite")]
public sealed class SqliteCombinatorialTransactionTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public SqliteCombinatorialTransactionTests(SqliteFixture fixture)
    {
        _fixture = fixture;
        RandomSchema.EnsureCreated(fixture);
    }

    [Fact]
    public void RunInRolledBackTransaction_Update_RestoresTheRows()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        StatementRunner runner = new(connection, Dbms.Sqlite);
        RandomUsersTable u = new("");
        PlannedStatement read = new(
            "READ",
            "Select(u.Id, u.Age).From(u).OrderBy(u.Id)",
            () => Select(u.Id, u.Age).From(u).OrderBy(u.Id));
        PlannedStatement update = new(
            "UPDATE",
            "Update(u).Set(u.Age == u.Age + 100).Where(u.DeptId == 1)",
            () => Update(u).Set(u.Age == u.Age + 100).Where(u.DeptId == 1));

        Outcome before = runner.Run(read);
        (Outcome updated, Outcome during) = runner.RunInRolledBackTransaction(transaction =>
            (runner.Run(update, transaction), runner.Run(read, transaction)));
        Outcome after = runner.Run(read);

        Assert.Null(updated.Error);
        Assert.Equal(RandomSchema.Users.Length, before.Rows!.Count);
        Assert.Equal(
            RandomSchema.Users.Select(r =>
                $"{r.Id}|{ReferenceEvaluator.Normalize(r.DeptId == 1 ? r.Age + 100 : r.Age)}"),
            during.Rows);
        Assert.Equal(before.Rows, after.Rows);
    }
}
