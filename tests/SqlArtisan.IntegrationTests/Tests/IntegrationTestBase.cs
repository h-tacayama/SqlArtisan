using System.Data;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// The dialect-agnostic core of the matrix: representative statements that are
/// valid on every engine. Each test builds with SqlArtisan and executes against
/// the real database, asserting it runs and returns the expected result — the
/// confidence the exact-SQL unit tests cannot give. Engine-specific syntax
/// (pagination, UPSERT, MERGE, RETURNING) lives in the per-engine subclasses.
///
/// Read tests rely on the baseline seed (5 users, 5 orders); mutating tests run
/// inside a rolled-back transaction so they never disturb that baseline,
/// keeping the suite order-independent.
/// </summary>
public abstract class IntegrationTestBase
{
    private readonly IDatabaseFixture _fixture;

    protected IntegrationTestBase(IDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void SelectWhere_FiltersAndBindsParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        List<string> names = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Age > 35).OrderBy(u.Name))
            .ToList();

        Assert.Equal(new[] { "Bob", "Carol" }, names);
    }

    [Fact]
    public void InnerJoin_MatchesRows()
    {
        OrdersTable o = new("o");
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(o.Id)).From(o).InnerJoin(u).On(o.UserId == u.Id)));

        Assert.Equal(5, count);
    }

    [Fact]
    public void Cte_FiltersThenSelects()
    {
        UsersTable u = new();
        Cte seniors = new("seniors");
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            With(seniors.As(Select(u.Id.As(seniors.Column("id"))).From(u).Where(u.Age >= 40)))
                .Select(Count(seniors.Column("id")))
                .From(seniors)));

        Assert.Equal(2, count);
    }

    [Fact]
    public void WindowFunction_RowNumber_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        List<int> rowNumbers = connection
            .Query<int>(Select(RowNumber().Over(PartitionBy(u.DepartmentId).OrderBy(u.Age))).From(u))
            .ToList();

        Assert.Equal(5, rowNumbers.Count);
        Assert.Equal(2, rowNumbers.Max());
    }

    [Fact]
    public void GroupByHaving_FiltersGroups()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        List<int> departments = connection
            .Query<int>(
                Select(u.DepartmentId)
                    .From(u)
                    .GroupBy(u.DepartmentId)
                    .Having(Count(u.Id) >= 2)
                    .OrderBy(u.DepartmentId))
            .ToList();

        Assert.Equal(new[] { 10, 20 }, departments);
    }

    [Fact]
    public void ScalarFunction_Upper_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string name = connection
            .Query<string>(Select(Upper(u.Name)).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("ALICE", name);
    }

    [Fact]
    public void Aggregate_Sum_Executes()
    {
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        decimal total = Convert.ToDecimal(connection.ExecuteScalar(Select(Sum(o.Amount)).From(o)));

        Assert.Equal(725m, total);
    }

    [Fact]
    public void Update_ModifiesRow()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(100, "Temp", 20, 99),
            transaction);
        connection.Execute(Update(u).Set(u.Name == "Updated").Where(u.Id == 100), transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 100), transaction)
            .Single();

        Assert.Equal("Updated", name);
        transaction.Rollback();
    }

    [Fact]
    public void Delete_RemovesRow()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(101, "Temp", 20, 99),
            transaction);
        connection.Execute(DeleteFrom(u).Where(u.Id == 101), transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == 101), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }
}
