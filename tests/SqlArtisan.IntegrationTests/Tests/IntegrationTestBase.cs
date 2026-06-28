using System.Data;
using System.Threading.Tasks;
using SqlArtisan;
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
    public void LeftJoin_IncludesUnmatchedRows()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();

        // Users {1..5} LEFT JOIN orders (user_ids 1, 1, 2, 3, 5): the four
        // matched users contribute five rows and unmatched Dave (4) contributes
        // one NULL-padded row — six in total. INNER JOIN would yield only five.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).LeftJoin(o).On(o.UserId == u.Id)));

        Assert.Equal(6, count);
    }

    [Fact]
    public void CrossJoin_ProducesCartesianProduct()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();

        // Five users × five orders = 25 unrestricted combinations.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).CrossJoin(o)));

        Assert.Equal(25, count);
    }

    [Fact]
    public void SelectDistinct_RemovesDuplicates()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // department_id values are {10, 10, 20, 20, 30}; DISTINCT collapses them
        // to the three distinct departments.
        IEnumerable<int> departments = connection
            .Query<int>(Select(Distinct, u.DepartmentId).From(u));

        Assert.Equal(3, departments.Count());
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
    public void Where_Between_FiltersRange()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Ages {30, 40, 50, 25, 35}; BETWEEN 30 AND 40 keeps Alice, Bob, Eve.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Age.Between(30, 40))));

        Assert.Equal(3, count);
    }

    [Fact]
    public void Where_NotBetween_FiltersOutsideRange()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Outside [30, 40]: Carol (50) and Dave (25).
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Age.NotBetween(30, 40))));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Where_Like_MatchesPattern()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Name.Like("A%")))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void Where_NotLike_ExcludesPattern()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Only Alice starts with 'A'; the other four remain.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Name.NotLike("A%"))));

        Assert.Equal(4, count);
    }

    [Fact]
    public void Where_NotInList_Filters()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Excluding ids {1, 2} leaves {3, 4, 5}.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id.NotIn(1, 2))));

        Assert.Equal(3, count);
    }

    [Fact]
    public void Where_NotInSubquery_Filters()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Orders reference users {1, 2, 3, 5}; only Dave (4) is NOT IN that set.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Where(u.Id.NotIn(Select(o.UserId).From(o))))
            .Single();

        Assert.Equal(4, id);
    }

    [Fact]
    public void Where_IsNullAndIsNotNull_Filter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Add one NULL-name row; the five seeded users all have names.
        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(150, null!, 20, 99),
            transaction);

        long nulls = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Name.IsNull), transaction));
        long nonNulls = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Name.IsNotNull), transaction));

        Assert.Equal(1, nulls);
        Assert.Equal(5, nonNulls);
        transaction.Rollback();
    }

    [Fact]
    public void Where_LogicalNot_Negates()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // NOT (age > 40) keeps everyone aged <= 40 — all but Carol (50).
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(Not(u.Age > 40))));

        Assert.Equal(4, count);
    }

    [Fact]
    public void OrderBy_Desc_SortsDescending()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id.Desc));

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, ids);
    }

    [Fact]
    public void Where_CompoundAnd_Filters()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // age > 30 AND department_id = 10 → only Bob (40, dept 10).
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Age > 30 & u.DepartmentId == 10)));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Where_CompoundOr_Filters()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // age > 45 OR department_id = 30 → Carol (50) and Eve (dept 30).
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Age > 45 | u.DepartmentId == 30)));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Arithmetic_Operators_Compute()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Alice is 30: 30 * 3 / 2 - 1 = 44, exercising *, /, and -.
        int value = connection
            .Query<int>(Select(u.Age * 3 / 2 - 1).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal(44, value);
    }

    [Fact]
    public void Where_DateTimeParameter_Matches()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The seed sets every user's created_at to this value; binding it as a
        // DateTime parameter must match all five rows.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.CreatedAt == new DateTime(2020, 3, 15, 10, 30, 0))));

        Assert.Equal(5, count);
    }

    [Fact]
    public void Where_DecimalParameter_Filters()
    {
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Amounts {100, 200, 50, 300, 75}; > 100.00 keeps 200 and 300.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(o.Id)).From(o).Where(o.Amount > 100.00m)));

        Assert.Equal(2, count);
    }

    [Fact]
    public virtual void Where_BooleanParameter_Filters()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // The seeded users leave is_active NULL; only the inserted active row
        // matches the bound boolean parameter.
        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.IsActive).Values(170, "Active", 20, 99, true),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.IsActive == true), transaction));

        Assert.Equal(1, count);
        transaction.Rollback();
    }

    [Fact]
    public void Aggregate_CountDistinct_Counts()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // department_id values {10, 10, 20, 20, 30} → three distinct.
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(Distinct, u.DepartmentId)).From(u)));

        Assert.Equal(3, count);
    }

    [Fact]
    public void GroupBy_MultipleColumns_Groups()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Each (department_id, age) pair is unique among the five users.
        int groups = connection
            .Query<int>(Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId, u.Age))
            .Count();

        Assert.Equal(5, groups);
    }

    [Fact]
    public void Update_SetExpression_Increments()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Alice is 30; SET age = age + 1 makes her 31.
        connection.Execute(Update(u).Set(u.Age == u.Age + 1).Where(u.Id == 1), transaction);

        int age = connection
            .Query<int>(Select(u.Age).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal(31, age);
        transaction.Rollback();
    }

    [Fact]
    public virtual void EdgeCase_BooleanFalse_RoundTrip()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.IsActive).Values(171, "Inactive", 20, 99, false),
            transaction);

        bool active = connection
            .Query<bool>(Select(u.IsActive).From(u).Where(u.Id == 171), transaction)
            .Single();

        Assert.False(active);
        transaction.Rollback();
    }

    [Fact]
    public async Task Async_QueryAndExecute_RoundTrip()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Exercises the async Dapper path end to end: ExecuteAsync (insert),
        // ExecuteScalarAsync (count), and QueryAsync (read back).
        await connection.ExecuteAsync(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(160, "Async", 20, 99),
            transaction);

        long count = Convert.ToInt64(await connection.ExecuteScalarAsync(
            Select(Count(u.Id)).From(u).Where(u.Id == 160), transaction));
        IEnumerable<string> names = await connection.QueryAsync<string>(
            Select(u.Name).From(u).Where(u.Id == 160), transaction);

        Assert.Equal(1, count);
        Assert.Equal("Async", names.Single());
        transaction.Rollback();
    }

    [Fact]
    public void DbTable_AdHocReference_Executes()
    {
        // An ad-hoc table reference (no typed DbTableBase subclass): name the
        // table inline and read columns by name.
        DbTable users = new("users", "u");
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(users.Column("id"))).From(users).Where(users.Column("id") == 1)));

        Assert.Equal(1, count);
    }

    [Fact]
    public void DapperEntryPoints_QueryVariants_Execute()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The thin Dapper passthroughs beyond Query/Execute/ExecuteScalar.
        int single = connection.QuerySingle<int>(Select(u.Id).From(u).Where(u.Id == 1));
        int first = connection.QueryFirst<int>(Select(u.Id).From(u).OrderBy(u.Id));
        int? none = connection.QueryFirstOrDefault<int?>(Select(u.Id).From(u).Where(u.Id == -1));
        int singleOrDefault = connection.QuerySingleOrDefault<int>(Select(u.Id).From(u).Where(u.Id == 1));

        Assert.Equal(1, single);
        Assert.Equal(1, first);
        Assert.Null(none);
        Assert.Equal(1, singleOrDefault);

        using (IDataReader reader = connection.ExecuteReader(Select(u.Id).From(u).Where(u.Id == 1)))
        {
            Assert.True(reader.Read());
        }

        using (var grid = connection.QueryMultiple(Select(u.Id).From(u).Where(u.Id == 1)))
        {
            Assert.Equal(1, grid.Read<int>().Single());
        }
    }

    [Fact]
    public async Task DapperEntryPoints_AsyncQueryVariants_Execute()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The async siblings of the passthroughs above — each builds for the
        // connection's dialect and dispatches to the matching Dapper async call.
        int single = await connection.QuerySingleAsync<int>(Select(u.Id).From(u).Where(u.Id == 1));
        int singleOrDefault = await connection.QuerySingleOrDefaultAsync<int>(Select(u.Id).From(u).Where(u.Id == 1));
        int first = await connection.QueryFirstAsync<int>(Select(u.Id).From(u).OrderBy(u.Id));
        int? none = await connection.QueryFirstOrDefaultAsync<int?>(Select(u.Id).From(u).Where(u.Id == -1));

        Assert.Equal(1, single);
        Assert.Equal(1, singleOrDefault);
        Assert.Equal(1, first);
        Assert.Null(none);

        using (IDataReader reader = await connection.ExecuteReaderAsync(Select(u.Id).From(u).Where(u.Id == 1)))
        {
            Assert.True(reader.Read());
        }

        using (var grid = await connection.QueryMultipleAsync(Select(u.Id).From(u).Where(u.Id == 1)))
        {
            Assert.Equal(1, grid.Read<int>().Single());
        }
    }

    [Fact]
    public void Api_FunctionSmoke() => RunSmoke(SmokeCatalog.Cases, "function");

    [Fact]
    public void Api_StatementSmoke() => RunSmoke(StatementCatalog.Cases, "statement");

    // Runs every catalog case valid for this engine and aggregates the failures,
    // so one run reports exactly which construct fails on which engine.
    private void RunSmoke(IReadOnlyList<SmokeCase> cases, string label)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        List<string> failures = [];
        int total = 0;
        foreach (SmokeCase smokeCase in cases)
        {
            if (!smokeCase.Engines.Contains(_fixture.Dbms))
            {
                continue;
            }

            total++;
            try
            {
                // ExecuteScalar runs the query server-side and reads one raw cell
                // — enough to prove the SQL executes, while avoiding Dapper's typed
                // result materialization (which can choke boxing provider-native
                // numerics, e.g. Oracle NUMBER from a window frame). We assert the
                // SQL runs, not that .NET can map the result.
                connection.ExecuteScalar(smokeCase.Build());
            }
            catch (Exception ex)
            {
                failures.Add($"{smokeCase.Name}: {ex.Message.Split('\n')[0].Trim()}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{_fixture.Dbms} {label} smoke: {failures.Count}/{total} failed:\n  "
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

    [Fact]
    public void InsertSelect_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Select(u.Id + 1000, u.Name)
                .From(u)
                .Where(u.Id == 1),
            transaction);

        long inserted = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == 1001), transaction));

        Assert.Equal(1, inserted);
        transaction.Rollback();
    }

    [Fact]
    public virtual void MultiRowValues_Executes()
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

    [Fact]
    public void Insert_NullValue_Executes()
    {
        // Regression for #169: Values(null) emits a SQL NULL literal (was a
        // NullReferenceException at build time).
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(120, null!, 20, 99),
            transaction);

        string? name = connection
            .Query<string?>(Select(u.Name).From(u).Where(u.Id == 120), transaction)
            .Single();

        Assert.Null(name);
        transaction.Rollback();
    }

    [Fact]
    public virtual void EdgeCase_Boolean_RoundTrip()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId, u.IsActive).Values(140, "B", 20, 99, true),
            transaction);

        bool active = connection
            .Query<bool>(Select(u.IsActive).From(u).Where(u.Id == 140), transaction)
            .Single();

        Assert.True(active);
        transaction.Rollback();
    }

    [Fact]
    public void EdgeCase_DateTime_RoundTrip()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The seed sets every user's created_at to this value (no fractional
        // seconds, so it round-trips on second-precision engines too).
        DateTime value = connection
            .Query<DateTime>(Select(u.CreatedAt).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal(new DateTime(2020, 3, 15, 10, 30, 0), value);
    }
}
