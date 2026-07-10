using System.Data;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlTests : IntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlTests(PostgreSqlFixture fixture) : base(fixture) => _fixture = fixture;

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
    public void Sequence_NextvalCurrval_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // One row, so NEXTVAL is called once; CURRVAL then reads it back in the
        // same session.
        long next = Convert.ToInt64(connection.ExecuteScalar(
            Select(Nextval("test_seq")).From(u).Where(u.Id == 1)));
        long current = Convert.ToInt64(connection.ExecuteScalar(
            Select(Currval("test_seq")).From(u).Where(u.Id == 1)));

        Assert.Equal(next, current);
    }

    [Fact]
    public void Upsert_OnConflictDoUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Values(1, "AliceUpdated")
                .OnConflict(u.Id)
                .DoUpdateSet(u.Name == Excluded(u.Name)),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("AliceUpdated", name);
        transaction.Rollback();
    }

    [Fact]
    public void Upsert_OnConflictDoNothing_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // id 1 already exists; DO NOTHING leaves the original row untouched.
        connection.Execute(
            InsertInto(u, u.Id, u.Name).Values(1, "ShouldNotApply").OnConflict(u.Id).DoNothing(),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("Alice", name);
        transaction.Rollback();
    }

    [Fact]
    public void Returning_OnInsert_ReadsBackRow()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        int id = connection
            .Query<int>(
                InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId)
                    .Values(200, "New", 20, 1)
                    .Returning(u.Id),
                transaction)
            .Single();

        Assert.Equal(200, id);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_StringAgg_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(StringAgg(u.Name, ",")).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void AggregateFilter_CountFilterWhere_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Ages are 30, 40, 50, 25, 35; three exceed 30. PostgreSQL supports FILTER.
        int matching = connection
            .Query<int>(Select(Count(u.Id).Filter(u.Age > 30)).From(u))
            .Single();

        Assert.Equal(3, matching);
    }

    [Fact]
    public void DistinctOn_OneRowPerDepartment_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Departments 10 -> {1,2}, 20 -> {3,4}, 30 -> {5}. DISTINCT ON
        // (department_id) ordered by (department_id, id) keeps the lowest id per
        // department: {1, 3, 5}.
        IEnumerable<int> ids = connection
            .Query<int>(
                Select(DistinctOn(u.DepartmentId), u.Id)
                    .From(u)
                    .OrderBy(u.DepartmentId, u.Id));

        Assert.Equal(new[] { 1, 3, 5 }, ids);
    }

    [Fact]
    public void SetOperator_Except_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Users {1..5} EXCEPT the users referenced by orders {1,2,3,5} = {4}.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Except.Select(o.UserId).From(o))
            .Single();

        Assert.Equal(4, id);
    }

    [Fact]
    public void JsonArrowText_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data ->> 'name') on the JSONB column; the key binds as a text parameter.
        string name = connection
            .Query<string>(Select(JsonArrowText(u.Data, "name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonArrow_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data -> 'address') returns the nested JSON object.
        string address = connection
            .Query<string>(Select(JsonArrow(u.Data, "address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact]
    public void JsonHashArrowText_ReadsByPath()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data #>> '{address,zip}') walks a path; PostgreSQL's #>> takes a text[]
        // right operand, so the path literal is cast to text[].
        string zip = connection
            .Query<string>(
                Select(JsonHashArrowText(u.Data, Cast("{address,zip}", "text[]")))
                    .From(u)
                    .Where(u.Id == 1))
            .Single();

        Assert.Equal("10001", zip);
    }

    [Fact]
    public void FullTextSearch_TsMatch_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // to_tsvector/plainto_tsquery are functional without a GIN index — the
        // index is a performance prerequisite, not a grammatical one.
        string name = connection
            .Query<string>(
                Select(u.Name)
                    .From(u)
                    .Where(TsMatch(
                        ToTsvector("english", u.Name),
                        PlaintoTsquery("english", "alice"))))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact] // #241 (GAP-19) probe: the same CASE instance in SELECT and GROUP BY
           // mints fresh bind slots per occurrence — records PG's accept/reject
           // (hypothesis: 42803, grouping matches expressions syntactically).
    public void GroupBy_SharedBindExpression_Executes()
    {
        UsersTable u = new();
        SqlExpression label =
            Case(u.DepartmentId, When(10).Then("Low"), When(20).Then("Mid"), Else("Other"));
        using IDbConnection connection = _fixture.OpenConnection();

        int groups = connection
            .Query<string>(Select(label).From(u).GroupBy(label))
            .Count();

        Assert.Equal(3, groups);
    }

    [Fact] // #241 (GAP-19) probe: the CTE-wrap escape — the bind values occur once
           // inside the CTE body, so GROUP BY references only the projected column.
    public void GroupBy_SharedBindExpression_CteWrap_Executes()
    {
        UsersTable u = new();
        Cte labeled = new("labeled");
        using IDbConnection connection = _fixture.OpenConnection();

        int groups = connection
            .Query<string>(
                With(labeled.As(
                    Select(
                        Case(u.DepartmentId, When(10).Then("Low"), When(20).Then("Mid"), Else("Other"))
                            .As(labeled.Column("label")))
                    .From(u)))
                .Select(labeled.Column("label"))
                .From(labeled)
                .GroupBy(labeled.Column("label")))
            .Count();

        Assert.Equal(3, groups);
    }
}
