using System.Data;
using Dapper;
using Npgsql;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlTests : IntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact] // #401: PostgreSQL resolves a MERGE action clause's column names against
           // the target table alone, so any qualifier there is read as a column name
           // and fails — the raw controls below pin both halves of that.
    public void MergeAliasedTargetColumns_Executes()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING users AS s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET t.name = s.name"));
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING users AS s ON t.id = s.id "
                + "WHEN NOT MATCHED THEN INSERT (t.id, t.name) VALUES (s.id, s.name)"));

        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t)
            .Using(s)
            .On(t.Id == s.Id)
            .WhenMatched().ThenUpdateSet(t.Name == s.Name)
            .WhenNotMatched().ThenInsert(t.Id, t.Name).Values(s.Id, s.Name),
            transaction);

        transaction.Rollback();
    }

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

    [Fact] // #241 (GAP-19): PostgreSQL matches GROUP BY expressions syntactically,
           // so a parameterized SELECT expression repeated with fresh markers fails
           // with 42803 (live-verified). Raw SQL by necessity — SqlArtisan now
           // reuses a shared instance's markers and cannot emit this form.
    public void GroupByBindMarkerMismatch_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The expression itself is valid (table and columns are right).
        connection.Execute(
            "SELECT CASE department_id WHEN @p0 THEN @p1 ELSE @p2 END FROM users",
            new { p0 = 10, p1 = "Low", p2 = "Other" });

        // The only difference — distinct markers in GROUP BY — is what PG rejects.
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "SELECT CASE department_id WHEN @p0 THEN @p1 ELSE @p2 END FROM users "
                + "GROUP BY CASE department_id WHEN @p3 THEN @p4 ELSE @p5 END",
            new { p0 = 10, p1 = "Low", p2 = "Other", p3 = 10, p4 = "Low", p5 = "Other" }));
    }

    [Fact] // ADR 0012 (#295): anchors the "no engine accepts it" premise — raw
           // SQL by necessity, since PercentileFractionGuard now rejects this client-side.
    public void PercentileCont_FractionOutOfRange_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The in-range form is valid (so the table and column are right).
        connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) FROM users");

        // The only difference — an out-of-range fraction — is what PG rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(1.5) WITHIN GROUP (ORDER BY age) FROM users"));
    }

    [Fact] // ADR 0012 (#402): anchors the "no engine accepts it" premise for the
           // window-frame value-domain guards — raw SQL by necessity, since
           // WindowFrameGuard now rejects each of these client-side.
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar("SELECT NTH_VALUE(age, 1) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 PRECEDING) FROM users");

        // The only difference each time — the value-domain violation — is what PG rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT NTILE(0) OVER (ORDER BY age) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT NTH_VALUE(age, 0) OVER (ORDER BY age) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS -1 PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS 1 FOLLOWING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN CURRENT ROW AND 1 PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED FOLLOWING AND UNBOUNDED FOLLOWING) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors the PostgreSqlExtractFields list in
           // DatepartValidity.cs — WEEKDAY is a SQL Server/MySQL spelling, not a
           // PostgreSQL EXTRACT field (PostgreSQL's day-of-week fields are DOW/ISODOW).
    public void Extract_WeekdayField_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed field is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT EXTRACT(EPOCH FROM created_at) FROM users");

        // The only difference — the field EXTRACT doesn't have on PostgreSQL —
        // is what PostgreSQL rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT EXTRACT(WEEKDAY FROM created_at) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors the PostgreSqlDateTruncFields list in
           // DatepartValidity.cs — EPOCH is EXTRACT-only; date_trunc has no epoch
           // field to truncate to.
    public void DateTrunc_EpochField_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed field is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT DATE_TRUNC('month', created_at) FROM users");

        // The only difference — the field date_trunc doesn't have — is what
        // PostgreSQL rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT DATE_TRUNC('epoch', created_at) FROM users"));
    }

    [Fact]
    public void JoinedUpdateFrom_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).Set(u.Age == 999).From(o).Where((u.Id == o.UserId) & (u.Id == 3)),
            transaction);

        int age = connection
            .Query<int>(Select(read.Age).From(read).Where(read.Id == 3), transaction)
            .Single();

        Assert.Equal(999, age);
        transaction.Rollback();
    }

    [Fact]
    public void JoinedDeleteUsing_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            DeleteFrom(u).Using(o).Where((u.Id == o.UserId) & (u.Id == 3)),
            transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(read.Id)).From(read).Where(read.Id == 3), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }

    [Fact]
    public void Where_AnyBindArray_FiltersByArrayParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The whole array travels as ONE parameter (ArrayQueryParameter must
        // bypass Dapper's IN-list expansion for = ANY (:0) to survive).
        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == Any(BindArray([1, 2, 4])))));

        Assert.Equal(3, count);
    }

    [Fact]
    public void Where_AnyBindArray_EmptyArray_MatchesNoRows()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long count = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id))
            .From(u)
            .Where(u.Id == Any(BindArray(System.Array.Empty<int>())))));

        Assert.Equal(0, count);
    }

    [Fact]
    public void From_UnnestBindArray_ExpandsIntoRows()
    {
        UnnestDerivedTable t = Unnest(BindArray([30, 10, 20])).AsTable("v");
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> values = connection
            .Query<int>(Select(t.Column("v")).From(t).OrderBy(t.Column("v")));

        Assert.Equal(new[] { 10, 20, 30 }, values);
    }

    [Fact]
    public async Task L2Distance_BoundVector_OrderByRoundTrips()
    {
        // Sql.Bind's allowlist excludes Pgvector.Vector by design; the sanctioned
        // route for a held provider-specific value is the public BindValue
        // constructor, whose raw value reaches Dapper's type handlers.
        global::Dapper.SqlMapper.AddTypeHandler(new Pgvector.Dapper.VectorTypeHandler());

        // A plain NpgsqlConnection cannot serialize Pgvector.Vector — the data
        // source must opt in via UseVector().
        NpgsqlDataSourceBuilder dataSourceBuilder = new(_fixture.ConnectionString);
        dataSourceBuilder.UseVector();
        await using NpgsqlDataSource dataSource = dataSourceBuilder.Build();
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        connection.Execute("CREATE EXTENSION IF NOT EXISTS vector");
        // The data source loaded its type catalog before the extension existed;
        // without a reload, writing Pgvector.Vector fails to resolve 'vector'.
        await connection.ReloadTypesAsync();
        connection.Execute("CREATE TABLE vector_probe (id integer, embedding vector(3))");
        try
        {
            connection.Execute(
                "INSERT INTO vector_probe VALUES (1, '[0,0,0]'), (2, '[1,1,1]')");

            VectorProbeTable t = new();
            Pgvector.Vector query = new(new float[] { 0.9f, 0.9f, 0.9f });
            IEnumerable<int> ids = connection.Query<int>(
                Select(t.Id)
                .From(t)
                .OrderBy(L2Distance(t.Embedding, new BindValue(query))));

            Assert.Equal(new[] { 2, 1 }, ids);
        }
        finally
        {
            connection.Execute("DROP TABLE vector_probe");
        }
    }

    private sealed class VectorProbeTable : DbTableBase
    {
        public VectorProbeTable(string alias = "") : base("vector_probe", alias)
        {
            Id = new DbColumn(this, "id");
            Embedding = new DbColumn(this, "embedding");
        }

        public DbColumn Id { get; }

        public DbColumn Embedding { get; }
    }
}
