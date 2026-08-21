using System.Data;
using System.Threading.Tasks;
using Dapper;
using SqlArtisan;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "Oracle")]
public sealed class OracleTests : IntegrationTestBase, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleTests(OracleFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Pagination_OffsetFetch_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection
            .Query<int>(Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(2));

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact] // Regression for #165: a re-aliased CTE column now emits a bare alias,
           // so it resolves on Oracle (previously ORA-00904).
    public void Cte_AliasedColumn_Executes()
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

    // Oracle XE 21c (the Testcontainers image) has no native boolean type
    // (is_active is NUMBER(1)), and binding a C# bool there is a driver concern
    // rather than a SqlArtisan one. The four engines with a native boolean type
    // cover the round-trip.
    [Fact(Skip = "Oracle XE 21c has no native boolean type; is_active is NUMBER(1).")]
    public override void EdgeCase_Boolean_RoundTrip()
    {
    }

    [Fact(Skip = "Oracle XE 21c has no native boolean type; is_active is NUMBER(1).")]
    public override void EdgeCase_BooleanFalse_RoundTrip()
    {
    }

    [Fact(Skip = "Oracle XE 21c has no native boolean type; is_active is NUMBER(1).")]
    public override void Where_BooleanParameter_Filters()
    {
    }

    [Fact(Skip = "Oracle has no multi-row VALUES; it uses INSERT ALL instead.")]
    public override void MultiRowValues_Executes()
    {
    }

    [Fact] // RETURNING ... INTO binds the affected columns into typed output
           // parameters; ExecuteReturningInto returns the populated bag so the
           // values can be read back after execution (the Oracle-specific form).
    public void ReturningInto_OnDelete_BindsOutputParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Carol is id 3; deleting that one row returns its id and name into the
        // numeric and string output parameters respectively.
        DynamicParameters outputs = connection.ExecuteReturningInto(
            DeleteFrom(u).Where(u.Id == 3)
                .Returning(u.Id, u.Name)
                .Into(new("outId", DbType.Int32), new("outName", DbType.String, 100)),
            transaction);

        Assert.Equal(3, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        Assert.Equal("Carol", outputs.Get<string>("outName"));
        transaction.Rollback();
    }

    [Fact] // The async counterpart of ReturningInto: ExecuteReturningIntoAsync
           // runs the statement and returns the populated bag.
    public async Task ReturningIntoAsync_OnDelete_BindsOutputParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // A live token, as in Async_QueryAndExecute_RoundTrip. This verb is the one that
        // does not go through ToCommand — it hand-builds its CommandDefinition to keep
        // the bag it returns — and this is where that hand-built command meets a real
        // engine; SqlMapperCancellationTests covers the verb in-process (#486).
        using CancellationTokenSource cts = new();

        DynamicParameters outputs = await connection.ExecuteReturningIntoAsync(
            DeleteFrom(u).Where(u.Id == 2)
                .Returning(u.Id).Into(new OutputParameter("outId", DbType.Int32)),
            transaction,
            cancellationToken: cts.Token);

        Assert.Equal(2, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        transaction.Rollback();
    }

    [Fact]
    public void Sequence_NextvalCurrval_Executes()
    {
        UsersTable u = new();
        var seq = Sequence("test_seq");
        using IDbConnection connection = _fixture.OpenConnection();

        // Oracle's pseudo-column form: test_seq.NEXTVAL / test_seq.CURRVAL.
        long next = Convert.ToInt64(connection.ExecuteScalar(
            Select(seq.Nextval).From(u).Where(u.Id == 1)));
        long current = Convert.ToInt64(connection.ExecuteScalar(
            Select(seq.Currval).From(u).Where(u.Id == 1)));

        Assert.Equal(next, current);
    }

    [Fact]
    public void Merge_UpsertViaMerge_Executes()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t)
                .Using(s)
                .On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name)
                .WhenNotMatched().ThenInsert(c.Id, c.Name).Values(s.Id, s.Name),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(5, count);
        transaction.Rollback();
    }

    [Fact] // Oracle's in-clause DELETE WHERE: the matched rows are updated, then
           // the just-updated rows satisfying the predicate are removed.
    public void Merge_DeleteWhere_RemovesUpdatedRows()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Every user matches; the UPDATE's DELETE WHERE then removes the updated
        // rows aged >= 50 (Carol), leaving four.
        connection.Execute(
            MergeInto(t)
                .Using(s)
                .On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name).DeleteWhere(t.Age >= 50),
            transaction);

        long count = Convert.ToInt64(connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(4, count);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_Listagg_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(Listagg(u.Name, ",").WithinGroup(OrderBy(u.Name))).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void SetOperator_Minus_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Oracle spells EXCEPT as MINUS: users {1..5} MINUS {1,2,3,5} = {4}.
        int id = connection
            .Query<int>(Select(u.Id).From(u).Minus.Select(o.UserId).From(o))
            .Single();

        Assert.Equal(4, id);
    }

    [Fact]
    public void JsonValue_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_VALUE(data, '$.name') extracts a scalar from the VARCHAR2 column.
        string name = connection
            .Query<string>(Select(JsonValue(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonQuery_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_QUERY(data, '$.address') returns the nested JSON object.
        string address = connection
            .Query<string>(Select(JsonQuery(u.Data, "$.address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact] // #255 / #239 (GAP-10, C5/C6): Oracle accepts the bare-separator DML
           // target alias `UPDATE users "cu"`, so a correlated subquery can
           // reference the outer row through the alias — the safe spelling that
           // avoids the unaliased tautology where the outer column silently
           // resolves to the inner table. Clears the grammar-unverified register
           // entry for the Oracle aliased correlated UPDATE.
    public void CorrelatedUpdate_AliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        OrdersTable o = new("o");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // department_id = 999 for exactly the users referenced by an order
        // ({1, 2, 3, 5}); the subquery correlates orders back to the outer
        // "cu".id, so without the alias the bare column would resolve to orders.
        connection.Execute(
            Update(cu)
                .Set(cu.DepartmentId == 999)
                .Where(cu.Id.In(Select(o.UserId).From(o).Where(o.UserId == cu.Id))),
            transaction);

        long updated = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.DepartmentId == 999), transaction));

        Assert.Equal(4, updated);
        transaction.Rollback();
    }

    [Fact] // #255 / #239 (GAP-10, C5/C6): the DELETE counterpart — Oracle accepts
           // the bare-separator DELETE target alias `DELETE FROM users "cu"`, so
           // the correlated subquery references the outer row safely. Clears the
           // grammar-unverified register entry for the Oracle aliased correlated
           // DELETE.
    public void CorrelatedDelete_AliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        OrdersTable o = new("o");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Delete the users referenced by an order ({1, 2, 3, 5}); Dave (4) has no
        // order and remains. The subquery correlates orders back to the outer
        // "cu".id.
        connection.Execute(
            DeleteFrom(cu)
                .Where(cu.Id.In(Select(o.UserId).From(o).Where(o.UserId == cu.Id))),
            transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u), transaction));

        Assert.Equal(1, remaining);
        transaction.Rollback();
    }

    [Fact] // #241 (GAP-19): the issue's original DECODE repro, kept Oracle-side as
           // the live check that a marker-reusing statement binds correctly through
           // Dapper + ODP.NET (the provider binds by position unless BindByName).
    public void GroupBy_SharedDecodeExpression_Executes()
    {
        UsersTable u = new();
        SqlExpression label = Decode(u.DepartmentId, (10, "Low"), (20, "Mid"), "Other");
        using IDbConnection connection = _fixture.OpenConnection();

        int groups = connection
            .Query<string>(Select(label).From(u).GroupBy(label))
            .Count();

        Assert.Equal(3, groups);
    }

    [Fact] // #241 (GAP-19): Oracle matches GROUP BY expressions syntactically, so a
           // parameterized SELECT expression repeated with fresh markers fails with
           // ORA-00979 (live-verified). Raw SQL by necessity — SqlArtisan now
           // reuses a shared instance's markers and cannot emit this form.
    public void GroupByBindMarkerMismatch_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The expression itself is valid (table and columns are right).
        connection.Execute(
            "SELECT DECODE(department_id, :p0, :p1, :p2) FROM users",
            new { p0 = 10, p1 = "Low", p2 = "Other" });

        // The only difference — distinct markers in GROUP BY — is what Oracle rejects.
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "SELECT DECODE(department_id, :p0, :p1, :p2) FROM users "
                + "GROUP BY DECODE(department_id, :p3, :p4, :p5)",
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

        // The only difference — an out-of-range fraction — is what Oracle rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(1.5) WITHIN GROUP (ORDER BY age) FROM users"));
    }

    [Fact] // ADR 0012 (#402): anchors the "no engine accepts it" premise for the
           // NTILE/NTH_VALUE/window-frame guards — raw SQL by necessity, since
           // WindowFrameGuard now rejects these client-side.
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar("SELECT NTH_VALUE(age, 1) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 PRECEDING) FROM users");

        // The only difference each time — the value-domain violation — is what Oracle rejects.
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

    [Fact] // SQLA0104 (#449): anchors the OracleExtractFields list in
           // DatepartValidity.cs — EPOCH is a PostgreSQL-only EXTRACT field.
    public void Extract_EpochField_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed field is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT EXTRACT(YEAR FROM created_at) FROM users");

        // The only difference — the field EXTRACT doesn't have on Oracle — is
        // what Oracle rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT EXTRACT(EPOCH FROM created_at) FROM users"));
    }

    [Fact] // ADR 0012 (#483): anchors the "no engine accepts it" premise for the
           // FOR UPDATE WAIT guard — raw SQL by necessity, since LockWaitGuard now
           // rejects a negative second count client-side.
    public void Wait_NegativeSeconds_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Both non-negative forms are valid (so the table is right, and 0 is the
        // floor the guard admits rather than an accident of the wait behavior).
        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 3");
        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 0");

        // The only difference — a negative second count — is what Oracle rejects
        // (ORA-30005), before any lock contention can matter.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT -1"));
    }

    [Fact] // ADR 0017: anchors the ISelectBuilderJoin guard (#420) — Oracle's ANSI
           // join grammar requires ON/USING for every listed join type except
           // CROSS JOIN, so the omission SqlArtisan now rejects at compile time was
           // always a syntax error here, never a silent wrong-result risk.
    public void OmittedJoinPredicate_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute("SELECT * FROM users CROSS JOIN orders");

        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users INNER JOIN orders"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users JOIN orders"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users LEFT JOIN orders"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users RIGHT JOIN orders"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users FULL JOIN orders"));
    }
}
