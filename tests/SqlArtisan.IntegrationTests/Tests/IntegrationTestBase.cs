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

        IEnumerable<string> names = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Age > 35).OrderBy(u.Name));

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

        // The CTE projects `id` without re-aliasing, so the column name folds
        // identically at definition and reference on every engine. Re-aliasing
        // (`.As(seniors.Column("id"))`) is broken on Oracle — see #165.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            With(seniors.As(Select(u.Id).From(u).Where(u.Age >= 40)))
                .Select(Count(seniors.Column("id")))
                .From(seniors)));

        Assert.Equal(2, count);
    }

    [Fact]
    public void WindowFunction_RowNumber_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> rowNumbers = connection
            .Query<int>(Select(RowNumber().Over(PartitionBy(u.DepartmentId).OrderBy(u.Age))).From(u));

        Assert.Equal(5, rowNumbers.Count());
        Assert.Equal(2, rowNumbers.Max());
    }

    [Fact]
    public void GroupByHaving_FiltersGroups()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> departments = connection
            .Query<int>(
                Select(u.DepartmentId)
                    .From(u)
                    .GroupBy(u.DepartmentId)
                    .Having(Count(u.Id) >= 2)
                    .OrderBy(u.DepartmentId));

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

    [Fact]
    public void SetOperator_Union_RemovesDuplicates()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // department_id values are {10, 10, 20, 20, 30}; UNION of the column with
        // itself collapses to the three distinct departments.
        IEnumerable<int> departments = connection
            .Query<int>(Select(u.DepartmentId).From(u).Union.Select(u.DepartmentId).From(u));

        Assert.Equal(3, departments.Count());
    }

    [Fact]
    public void SetOperator_UnionAll_KeepsDuplicates()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> departments = connection
            .Query<int>(Select(u.DepartmentId).From(u).UnionAll.Select(u.DepartmentId).From(u));

        Assert.Equal(10, departments.Count());
    }

    [Fact]
    public void WindowFunction_Rank_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Ages are all distinct, so RANK over age ascending yields 1..5.
        IEnumerable<int> ranks = connection
            .Query<int>(Select(Rank().Over(OrderBy(u.Age))).From(u));

        Assert.Equal(5, ranks.Max());
    }

    [Fact]
    public void WindowFunction_AggregateOver_Executes()
    {
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<decimal> partitionTotals = connection
            .Query<decimal>(Select(Sum(o.Amount).Over(PartitionBy(o.UserId))).From(o));

        Assert.Equal(5, partitionTotals.Count());
    }

    [Fact]
    public void WindowFunction_Lag_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The first row's LAG is NULL, so map to a nullable int.
        IEnumerable<int?> previousAges = connection
            .Query<int?>(Select(Lag(u.Age).Over(OrderBy(u.Id))).From(u));

        Assert.Equal(5, previousAges.Count());
    }

    [Fact]
    public void ScalarFunction_Coalesce_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string name = connection
            .Query<string>(Select(Coalesce(u.Name, "fallback")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void Subquery_InWhere_Filters()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Orders reference users {1, 2, 3, 5}, so four users have at least one order.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id.In(Select(o.UserId).From(o)))));

        Assert.Equal(4, count);
    }

    [Fact]
    public void Api_FunctionSmoke()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        List<string> failures = [];
        int total = 0;
        foreach (SmokeCase smokeCase in SmokeCatalog.Cases)
        {
            if (!smokeCase.Engines.Contains(_fixture.Dbms))
            {
                continue;
            }

            total++;
            try
            {
                connection.Query<object>(smokeCase.Build()).ToList();
            }
            catch (Exception ex)
            {
                failures.Add($"{smokeCase.Name}: {ex.Message.Split('\n')[0].Trim()}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{_fixture.Dbms} function smoke: {failures.Count}/{total} failed:\n  "
                + string.Join("\n  ", failures));
    }

    [Fact]
    public void EdgeCase_NullValue_CoalesceReturnsFallback()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // NULLIF(name, name) is always NULL, so COALESCE must fall through to the
        // fallback. (Produces the NULL in SQL rather than inserting one — see #169.)
        string value = connection
            .Query<string>(
                Select(Coalesce(Nullif(u.Name, u.Name), "fallback")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("fallback", value);
    }

    [Fact]
    public void EdgeCase_SpecialCharacters_RoundTrip()
    {
        UsersTable u = new();
        const string tricky = "O'Brien \"q\" %_\\ end";
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(111, tricky, 20, 99),
            transaction);

        string value = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 111), transaction)
            .Single();

        Assert.Equal(tricky, value);
        transaction.Rollback();
    }

    [Fact]
    public void EdgeCase_Unicode_RoundTrip()
    {
        UsersTable u = new();
        const string unicode = "日本語café";
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(112, unicode, 20, 99),
            transaction);

        string value = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 112), transaction)
            .Single();

        Assert.Equal(unicode, value);
        transaction.Rollback();
    }

    [Fact]
    public void EdgeCase_EmptyResult_ReturnsNoRows()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        bool any = connection.Query<int>(Select(u.Id).From(u).Where(u.Id == -1)).Any();

        Assert.False(any);
    }

    [Fact]
    public void EdgeCase_DecimalPrecision_RoundTrip()
    {
        OrdersTable o = new();
        const decimal amount = 12345.67m;
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(o, o.Id, o.UserId, o.Amount).Values(900, 1, amount),
            transaction);

        decimal value = connection
            .Query<decimal>(Select(o.Amount).From(o).Where(o.Id == 900), transaction)
            .Single();

        Assert.Equal(amount, value);
        transaction.Rollback();
    }
}
