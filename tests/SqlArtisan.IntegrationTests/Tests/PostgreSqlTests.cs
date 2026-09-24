using System.Data;
using System.Data.Common;
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

    [Fact] // #401: PostgreSQL resolves a MERGE action clause's column names against the
           // target alone, so any qualifier there is read as a column name and fails.
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

    // The live twin of InsertBuilder's conflict-target guard (ADR 0011).
    [Fact]
    public void Upsert_OnConflictDoUpdateWithoutTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "INSERT INTO users (id, name) VALUES (1, 'x') "
                    + "ON CONFLICT DO UPDATE SET name = EXCLUDED.name",
                transaction: transaction));
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

        // DISTINCT ON (department_id) ordered by (department_id, id) keeps the
        // lowest id per department.
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

    [Fact] // #241 (GAP-19): PostgreSQL matches GROUP BY syntactically, so a
           // parameterized expression repeated with fresh markers fails with 42803.
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

    [Fact] // ADR 0012 (#295): anchors PercentileFractionGuard.
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

    [Fact] // ADR 0012 (#402): anchors WindowFrameGuard.
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar("SELECT NTH_VALUE(age, 1) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 "
                + "PRECEDING) FROM users");

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
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN CURRENT ROW AND 1 "
                + "PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED PRECEDING "
                + "AND UNBOUNDED PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED FOLLOWING AND UNBOUNDED "
                + "FOLLOWING) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors PostgreSqlExtractFields in DatepartValidity.cs
           // — WEEKDAY is a SQL Server/MySQL spelling; PostgreSQL uses DOW/ISODOW.
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

    [Fact] // SQLA0104 (#449): anchors PostgreSqlDateTruncFields in DatepartValidity.cs
           // — EPOCH is EXTRACT-only; date_trunc has no epoch field to truncate to.
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
        // Sql.Bind's allowlist excludes Pgvector.Vector by design; the sanctioned route
        // is the public BindValue constructor, whose raw value reaches type handlers.
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

    // Live twins of the duplicate-list guards (guards-and-empty-states.md): the
    // guards reject the shape everywhere, so each lane pins what its engine does.
    [Fact]
    public void DuplicateInsertColumn_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Rolled back so a row cannot leak into the shared fixture if the claim fails.
        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "INSERT INTO users (id, id) VALUES (901, 902)",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void DuplicateSetAssignment_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "UPDATE users SET age = 1, age = 2 WHERE id = 1",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void DuplicateUsingColumn_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users JOIN orders USING (id, id)"));
    }

    [Fact]
    public void DuplicateCteColumnName_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        int count = connection
            .Query<int>("WITH x(a, a) AS (SELECT 1, 2) SELECT COUNT(*) FROM x")
            .Single();

        Assert.Equal(1, count);
    }

    [Fact]
    public void DuplicateCteName_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("WITH x AS (SELECT 1), x AS (SELECT 2) SELECT * FROM x"));
    }

    // #521 item 2: the GROUP BY constant verdicts here — the position works, an
    // out-of-range one does not, and a fractional key is refused outright
    // unlike on MySQL and SQLite.
    [Fact]
    public void GroupByOrdinal_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM (SELECT DISTINCT department_id FROM users) d"),
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM (SELECT department_id FROM users GROUP BY 1) g"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2.5")]
    public void GroupByOutOfRangeConstant_IsRejectedByTheEngine(string constant)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            $"SELECT department_id FROM users GROUP BY {constant}"));
    }

    // The live twin of SelectBuilder's non-integer sort-key guard (ADR 0011).
    [Fact]
    public void OrderByNonIntegerConstant_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY 2.5"));
    }

    // The live twins of the ORDER BY ordinal guards: zero is no column position
    // on any engine (ADR 0007); PostgreSQL reads a negative literal as one too.
    [Fact]
    public void OrderByZeroOrdinal_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY 0"));
    }

    [Fact]
    public void OrderByNegativeOrdinal_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY -1"));
    }

    // The nested twin: a CTE body resolves its own ordinals, which is why the
    // guard runs on every query block rather than the outermost one.
    [Fact]
    public void OrderByZeroOrdinalInCteBody_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("WITH c AS (SELECT id FROM users ORDER BY 0) SELECT id FROM c"));
    }

    // The live end of DateTimePartNumbering: the rows that make each summary's
    // numbering basis a measurement rather than an assertion (#523 item 7).
    [Fact]
    public void ExtractDayOfWeek_NumbersAsTheSummariesState()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        foreach ((string member, DateTimePartNumbering.NumberingClaim claim)
            in DateTimePartNumbering.Claims)
        {
            foreach ((string date, int expected) in claim.Rows)
            {
                int actual = connection.ExecuteScalar<int>(
                    $"SELECT EXTRACT({claim.Field} FROM DATE '{date}')");

                Assert.True(
                    expected == actual,
                    $"DateTimePart.{member} states \"{claim.Phrase}\", but "
                        + $"EXTRACT({claim.Field} FROM DATE '{date}') returned {actual}.");
            }
        }
    }

    // #523/#525: MERGE branch arity is a documented non-goal, not a guard. What
    // decides acceptance here is the condition, not the count.
    [Fact]
    public void MergeRepeatedWhenBranch_NeedsAConditionOnTheEarlierBranch()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name) s ON t.id = s.id "
                + "WHEN MATCHED AND t.age > 0 THEN UPDATE SET name = t.name "
                + "WHEN MATCHED THEN DELETE",
            transaction: transaction);

        // Drop the condition off the first branch and the second is unreachable.
        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name) s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = t.name "
                + "WHEN MATCHED THEN DELETE",
            transaction: transaction));

        transaction.Rollback();
    }

    // ADR 0012 non-goals (#523): the negative LAG offset PostgreSQL reads as a
    // LEAD is the accepting engine condition 1 needs; the row count it rejects.
    [Fact]
    public void LagNegativeOffset_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<int>("SELECT LEAD(id, 1) OVER (ORDER BY id) FROM users"),
            connection.ExecuteScalar<int>("SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"));
    }

    // SQLA0104's twin for all three row-count spellings (#529): each raises
    // `LIMIT must not be negative` here — the acceptance twin is
    // OracleTests.NegativeFetchCount.
    [Theory]
    [InlineData("SELECT id FROM users ORDER BY id FETCH FIRST -1 ROWS ONLY")]
    [InlineData("SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY")]
    [InlineData("SELECT id FROM users ORDER BY id LIMIT -1")]
    public void NegativeRowCount_IsRejectedByTheEngine(string statement)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(statement));
    }

    // Why SQLA0104 leaves the OFFSET family out is a decision, not a gap (#532):
    // the start reaches the engine as a bind parameter, and an offset goes
    // negative by arithmetic rather than at the call site the rule can read.
    [Theory]
    [InlineData("SELECT id FROM users ORDER BY id OFFSET -1")]
    [InlineData("SELECT id FROM users ORDER BY id OFFSET -1 ROWS")]
    public void NegativeOffset_IsRejectedByTheEngine(string statement)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute(statement.Replace("-1", "0"));

        Assert.ThrowsAny<DbException>(() => connection.Execute(statement));
    }

    // ADR 0012 non-goal (#523 item 1): the alphabet diverges per engine and a
    // contradictory pair is accepted everywhere, so no value-domain guard fits.
    [Theory]
    [InlineData("")]
    [InlineData("c")]
    [InlineData("i")]
    [InlineData("m")]
    [InlineData("n")]
    [InlineData("x")]
    public void RegexpMatchParameter_IsAcceptedByTheEngine(string flags)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(MatchParameterProbe(flags));
    }

    [Theory]
    [InlineData("u")] // MySQL's Unicode letter, which PostgreSQL has not.
    [InlineData("z")]
    public void RegexpMatchParameter_UnknownLetter_IsRejectedByTheEngine(string flags)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(MatchParameterProbe(flags)));
    }

    [Theory]
    [InlineData("ci", true)]
    [InlineData("ic", false)]
    public void RegexpContradictoryMatchParameter_ResolvesToTheLastLetter(
        string flags, bool expected)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(expected, connection.ExecuteScalar<bool>(MatchParameterProbe(flags)));
    }

    private static string MatchParameterProbe(string flags) =>
        $"SELECT REGEXP_LIKE('Ab', 'ab', '{flags}')";

    // #523: SQLA0102's live proofs. The DELETE ... USING and FROM-form UPDATE
    // spellings PostgreSQL does own are proven by JoinedDeleteUsing_Executes and
    // JoinedUpdateFrom_Executes above.
    [Fact]
    public void ContextRule_JoinedDeleteLead_Rejected()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            DeleteFrom(u).From(u, o).Where((u.Id == o.UserId) & (u.Id == 3)), transaction));
        transaction.Rollback();
    }

    [Fact]
    public void ContextRule_JoinedUpdateJoinForm_Rejected()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            Update(u).InnerJoin(o).On(u.Id == o.UserId).Set(u.Age == 999), transaction));
        transaction.Rollback();
    }

    [Fact] // SQLSTATE 0A000. The ungrouped lock is proven by the dialect sweep's
           // ForUpdate case, so the GROUP BY is the only difference.
    public void ContextRule_ForUpdateAfterGroupBy_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId)
                .OrderBy(u.DepartmentId).ForUpdate()));
    }

    // #520: why PostgreSQL is absent from the row-limiting FOR UPDATE context rule
    // — it runs both row-limiting families beside the lock, where Oracle rejects them.
    [Fact]
    public void LimitedForUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id).Limit(1).ForUpdate(SkipLocked), transaction);

        Assert.Equal(new[] { 1 }, ids);
        transaction.Rollback();
    }

    [Fact]
    public void OffsetFetchForUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(1).ForUpdate(),
            transaction);

        Assert.Equal(new[] { 2 }, ids);
        transaction.Rollback();
    }

    // The raw half of the docs' claim: PostgreSQL 16 takes a leading WITH before
    // MERGE, and refuses the RECURSIVE keyword there whatever the CTE body does.
    [Fact]
    public void LeadingWithBeforeMerge_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "WITH c AS (SELECT 1 AS id, 'x' AS name) "
                + "MERGE INTO users t USING c s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = s.name",
            transaction: transaction);
        transaction.Rollback();
    }

    [Fact]
    public void LeadingWithRecursiveBeforeMerge_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the same RECURSIVE keyword over the same
        // non-recursive body runs before a SELECT, so the refusal is MERGE's.
        connection.Query<int>(
            "WITH RECURSIVE c AS (SELECT 1 AS id) SELECT id FROM c",
            transaction: transaction).ToList();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "WITH RECURSIVE c AS (SELECT 1 AS id, 'x' AS name) "
                    + "MERGE INTO users t USING c s ON t.id = s.id "
                    + "WHEN MATCHED THEN UPDATE SET name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // The built half: the feature this lane exists to prove runs end to end.
    [Fact]
    public void Cte_LeadingWithBeforeMerge_Executes()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        Cte fresh = new("fresh");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        int merged = connection.Execute(
            With(fresh.As(Select(s.Id, s.Name).From(s).Where(s.Id <= 2)))
                .MergeInto(t)
                .Using(fresh)
                .On(t.Id == fresh.Column("id"))
                .WhenMatched().ThenUpdateSet(t.Name == fresh.Column("name")),
            transaction);

        Assert.Equal(2, merged);
        transaction.Rollback();
    }

    // #521: PostgreSQL's OF names the relation to lock; the joined one stays free.
    [Fact]
    public void ForUpdate_OfTable_LocksOnlyThatTable()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection a = _fixture.OpenConnection();
        using IDbTransaction ta = a.BeginTransaction();
        a.Query<int>(
            Select(u.Id).From(u).InnerJoin(o).On(o.UserId == u.Id).Where(u.Id == 1)
                .ForUpdate(Of(u)),
            ta).ToList();

        // A second session asking NOWAIT finds the order free and the user locked.
        using IDbConnection b = _fixture.OpenConnection();
        using IDbTransaction tb = b.BeginTransaction();
        b.Query<int>("SELECT id FROM orders WHERE id = 1 FOR UPDATE NOWAIT", transaction: tb)
            .ToList();
        Assert.ThrowsAny<DbException>(() => b.Query<int>(
            "SELECT id FROM users WHERE id = 1 FOR UPDATE NOWAIT", transaction: tb).ToList());
        tb.Rollback();
        ta.Rollback();
    }

    [Fact]
    public void ForUpdate_OfTwoTables_WithSkipLocked_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> ids = connection.Query<int>(
            Select(o.Id).From(u).InnerJoin(o).On(o.UserId == u.Id).Where(u.Id == 1)
                .OrderBy(o.Id).ForUpdate(Of(u, o), SkipLocked),
            transaction);

        Assert.Equal(new[] { 1, 2 }, ids);
        transaction.Rollback();
    }

    // PostgreSQL rejects a plain FOR UPDATE on an outer join; an OF naming the
    // preserved side runs — the claim the docs and Of(table)'s remarks make.
    [Fact]
    public void ForUpdate_OuterJoin_RunsOnlyWithOfThePreservedSide()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();

        using (IDbTransaction t = connection.BeginTransaction())
        {
            Assert.ThrowsAny<DbException>(() => connection.Query<int>(
                Select(u.Id).From(u).LeftJoin(o).On(o.UserId == u.Id).ForUpdate(), t).ToList());
            t.Rollback();
        }

        using IDbTransaction transaction = connection.BeginTransaction();
        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).LeftJoin(o).On(o.UserId == u.Id).Where(u.Id == 4)
                .ForUpdate(Of(u)),
            transaction);

        Assert.Equal(new[] { 4 }, ids);
        transaction.Rollback();
    }

    // Why Of(table) renders the alias: once a table is aliased, the engine
    // no longer takes its name in OF.
    [Fact]
    public void ForUpdateOfTableNameOfAliasedTable_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT u.id FROM users u JOIN orders o ON o.user_id = u.id FOR UPDATE OF users",
            transaction: transaction).ToList());
        transaction.Rollback();
    }

    // #521 (e) probe: a schema-qualified, unaliased table in FOR UPDATE OF.
    [Fact]
    public void ForUpdateOfQualifiedTable_Probe()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string schema = connection.ExecuteScalar<string>("SELECT current_schema()")!;
        string from = $"SELECT id FROM {schema}.users WHERE id = 1 ";

        using (IDbTransaction t = connection.BeginTransaction())
        {
            Assert.ThrowsAny<DbException>(() => connection.Query<int>(
                from + $"FOR UPDATE OF {schema}.users", transaction: t).ToList());
            t.Rollback();
        }

        using IDbTransaction transaction = connection.BeginTransaction();
        connection.Query<int>(from + "FOR UPDATE OF users", transaction: transaction).ToList();
        transaction.Rollback();
    }
}
