using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan;
using SqlArtisan.Analyzers;
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

    [Fact(Skip = "Oracle's DML grammar has no leading WITH; the form is documented for the other "
        + "four engines.")]
    public override void Cte_LeadingWithBeforeDelete_Executes()
    {
    }

    [Theory] // #521, #539: Oracle's rejection of the ranking family's unordered window,
             // dialect availability beside OrderBy(Null)'s same rows (#664).
    [InlineData("PARTITION BY department_id")]
    [InlineData("")]
    public void UnorderedWindow_RankingFamily_Rejected(string window)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The value family takes the same unordered window, so only the ranking
        // and offset functions are what Oracle 21c raises ORA-30485 for.
        connection.ExecuteScalar($"SELECT FIRST_VALUE(age) OVER ({window}) FROM users");

        string[] unordered =
        [
            "RANK()", "DENSE_RANK()", "ROW_NUMBER()", "NTILE(2)", "CUME_DIST()",
            "PERCENT_RANK()", "LAG(age)", "LEAD(age)",
        ];

        Assert.All(
            unordered,
            function => Assert.Contains(
                "ORA-30485",
                Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
                    $"SELECT {function} OVER ({window}) FROM users")).Message));
    }

    // Binding a C# bool to NUMBER(1) is a driver concern, not a SqlArtisan one;
    // the four engines with a native boolean type cover the round-trip.
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

    [Fact(Skip = "Oracle 21c rejects multi-row VALUES; MultiRowValues_IsRejected asserts "
        + "the rejection here, the 23ai lane (Oracle23aiTests) the acceptance.")]
    public override void MultiRowValues_Executes()
    {
    }

    // The rejecting half of the doc note's Oracle claim ("added in 23ai"): the
    // 21c engine must refuse the table value constructor, or the note is stale.
    [Fact]
    public void MultiRowValues_IsRejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            InsertInto(u, u.Id, u.Name).Values(201, "A").Values(202, "B")));
    }

    // SQLA0103 (#684): an alias at the analyzer's limit runs, and one character
    // past it is rejected.
    [Theory]
    [MemberData(nameof(IdentifierLimits.Characters), MemberType = typeof(IdentifierLimits))]
    public void Alias_PastTheIdentifierLimit_IsRejectedByTheEngine(char character)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string atLimit = IdentifierLimits.AtTheLimit(TargetDbms.Oracle, character);

        Assert.Equal(atLimit, IdentifierLimits.ReturnedAlias(connection, atLimit));
        Assert.ThrowsAny<DbException>(
            () => IdentifierLimits.ReturnedAlias(connection, atLimit + character));
    }

    // OutputParameter rejects a name not shaped as an identifier (#679): an output named "0 "
    // beside the positional :0 renders INTO :0 , which binds the returned id to that input.
    [Fact]
    public void ReturningInto_MarkerEndingInWhiteSpace_BindsTheInputMarker()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        DynamicParameters parameters = new();
        parameters.Add(":0", 77);
        parameters.Add(":1", 1);
        parameters.Add(":0 ", dbType: DbType.Int32, direction: ParameterDirection.Output);

        connection.Execute(
            "UPDATE users SET age = :0 WHERE id = :1 RETURNING id INTO :0 ",
            parameters,
            transaction);

        Assert.True(parameters.Get<object?>(":0 ") is null or DBNull);
        Assert.Equal(1, Convert.ToInt32(parameters.Get<object>(":0")));
        Assert.Equal(77, connection.ExecuteScalar<int>(
            "SELECT age FROM users WHERE id = 1", transaction: transaction));
        transaction.Rollback();
    }

    // The RETURNING ... INTO variable is the one name SQLA0103 checks on Oracle alone.
    [Fact]
    public void OutputParameter_PastTheIdentifierLimit_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        string atLimit = IdentifierLimits.AtTheLimit(TargetDbms.Oracle, 'r');
        ISqlBuilder Returning(string name) =>
            Update(u).Set(u.Age == u.Age).Where(u.Id == 1)
                .Returning(u.Id).Into(new OutputParameter(name, DbType.Int32));

        DynamicParameters outputs = connection.ExecuteReturningInto(
            Returning(atLimit), transaction);

        Assert.Equal(1, Convert.ToInt32(outputs.Get<object>(atLimit)!.ToString()));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteReturningInto(
            Returning(atLimit + 'r'), transaction));
        transaction.Rollback();
    }

    [Fact]
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

    [Fact]
    public async Task ReturningIntoAsync_OnDelete_BindsOutputParameter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // A live token, as in Async_QueryAndExecute_RoundTrip: only a real engine
        // populates the output bag this verb hands back (#486).
        using CancellationTokenSource cts = new();

        DynamicParameters outputs = await connection.ExecuteReturningIntoAsync(
            DeleteFrom(u).Where(u.Id == 2)
                .Returning(u.Id).Into(new OutputParameter("outId", DbType.Int32)),
            transaction,
            cancellationToken: cts.Token);

        Assert.Equal(2, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        transaction.Rollback();
    }

    // #675 item 7. The library's width check refuses * against three outputs, so
    // the width-matched form, which isolates the *, is raw text.
    [Fact]
    public void ReturningInto_Asterisk_IsRejectedByTheEngine()
    {
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        (string Statement, int UserId)[] cases =
        [
            ("INSERT INTO orders (id, user_id, amount) VALUES (90, 1, 10) RETURNING {0}", 1),
            ("UPDATE orders SET amount = amount WHERE id = 5 RETURNING {0}", 5),
            ("DELETE FROM orders WHERE id = 4 RETURNING {0}", 3),
        ];

        // Each probe runs before its control, so a rejection cannot be the control's key.
        foreach ((string statement, int userId) in cases)
        {
            Assert.ThrowsAny<DbException>(
                () => connection.Execute(Into(statement, "*"), Outputs(), transaction));
            Assert.ThrowsAny<DbException>(
                () => connection.Execute(Into(statement, "orders.*"), Outputs(), transaction));

            DynamicParameters outputs = Outputs();
            connection.Execute(Into(statement, "id, user_id, amount"), outputs, transaction);
            Assert.Equal(userId, Convert.ToInt32(outputs.Get<object>("outUserId")!.ToString()));
        }

        Assert.ThrowsAny<DbException>(() => connection.ExecuteReturningInto(
            DeleteFrom(o).Where(o.Id == 3).Returning(Asterisk)
                .Into(new OutputParameter("outId", DbType.Int32)),
            transaction));

        static string Into(string statement, string items) =>
            string.Format(statement, items) + " INTO :outId, :outUserId, :outAmount";

        static DynamicParameters Outputs()
        {
            DynamicParameters parameters = new();
            parameters.Add("outId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add(
                "outUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add(
                "outAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            return parameters;
        }
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

        long count = Convert.ToInt64(
            connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

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

        long count = Convert.ToInt64(
            connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

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

    [Fact] // #255 / #239 (GAP-10, C5/C6): Oracle accepts the bare-separator DML target
           // alias `UPDATE users "cu"`; without it the outer column resolves to orders.
    public void CorrelatedUpdate_AliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        OrdersTable o = new("o");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

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
           // `DELETE FROM users "cu"`; without it the outer column resolves to orders.
    public void CorrelatedDelete_AliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        OrdersTable o = new("o");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            DeleteFrom(cu)
                .Where(cu.Id.In(Select(o.UserId).From(o).Where(o.UserId == cu.Id))),
            transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u), transaction));

        Assert.Equal(1, remaining);
        transaction.Rollback();
    }

    [Fact] // #241 (GAP-19): the issue's original DECODE repro — ODP.NET binds by
           // position unless BindByName, so marker reuse needs a live check.
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

    // OracleDialect.DmlTableAliasSeparator is a space: Oracle rejects AS before a DML
    // target's alias, and runs the alias without it (#679).
    [Fact]
    public void DmlTargetAlias_WithAs_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        const string Set = " SET age = \"u\".age WHERE \"u\".id = 1";

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "UPDATE users AS \"u\"" + Set, transaction: transaction));
        Assert.Equal(1, connection.Execute("UPDATE users \"u\"" + Set, transaction: transaction));
        transaction.Rollback();
    }

    [Fact] // #241 (GAP-19): Oracle matches GROUP BY syntactically, so a parameterized
           // expression repeated with fresh markers fails with ORA-00979.
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

    [Fact] // ADR 0012 (#295): anchors PercentileFractionGuard.
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

    // ADR 0012 (#661): Oracle's fractional-seconds ceiling is its own, so the library
    // leaves a SECOND precision above 9 to it.
    [Fact]
    public void IntervalLiteral_SecondPrecisionAboveNine_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(
            Select(IntervalLiteral("1 2:3:4.5", Day(), ToSecond(9))).From(Dual));

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(IntervalLiteral("1 2:3:4.5", Day(), ToSecond(10))).From(Dual)));
    }

    // #664: Oracle runs LISTAGG without WITHIN GROUP, so its pending type rests on
    // WithinGroup(OrderBy(Null)) aggregating the same values, the order left to Oracle.
    [Fact]
    public void Listagg_WithoutWithinGroup_IsAcceptedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string bare = connection.QuerySingle<string>("SELECT LISTAGG(name, ',') FROM users");
        string unordered = connection.QuerySingle<string>(
            Select(Listagg(u.Name, ",").WithinGroup(OrderBy(Null))).From(u));

        Assert.Equal(
            bare.Split(',').Order(StringComparer.Ordinal),
            unordered.Split(',').Order(StringComparer.Ordinal));
    }

    // #664: a NULL delimiter is LISTAGG's default, the spelling public-api-design.md
    // records for a LISTAGG with no delimiter.
    [Fact]
    public void Listagg_NullDelimiter_MatchesTheDefault()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        string? byDefault = connection.QuerySingle<string>(
            "SELECT LISTAGG(name) WITHIN GROUP (ORDER BY name) FROM users");
        string? byNull = connection.QuerySingle<string>(
            "SELECT LISTAGG(name, NULL) WITHIN GROUP (ORDER BY name) FROM users");

        Assert.Equal(byDefault, byNull);
    }

    [Fact] // ADR 0012 (#661): anchors the non-negative CURRENT_TIMESTAMP precision guard.
    public void CurrentTimestamp_NegativePrecision_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT CURRENT_TIMESTAMP(3) FROM DUAL");

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT CURRENT_TIMESTAMP(-1) FROM DUAL"));
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
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN CURRENT ROW AND 1 "
                + "PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED PRECEDING "
                + "AND UNBOUNDED PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED FOLLOWING AND UNBOUNDED "
                + "FOLLOWING) FROM users"));
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

    [Fact] // ADR 0012 (#483): anchors LockWaitGuard.
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

    [Fact] // ADR 0017: anchors the ISelectBuilderJoin guard (#420).
    public void OmittedJoinPredicate_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute("SELECT * FROM users CROSS JOIN orders");

        Assert.ThrowsAny<Exception>(
            () => connection.Execute("SELECT * FROM users INNER JOIN orders"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT * FROM users JOIN orders"));
        Assert.ThrowsAny<Exception>(
            () => connection.Execute("SELECT * FROM users LEFT JOIN orders"));
        Assert.ThrowsAny<Exception>(
            () => connection.Execute("SELECT * FROM users RIGHT JOIN orders"));
        Assert.ThrowsAny<Exception>(
            () => connection.Execute("SELECT * FROM users FULL JOIN orders"));
    }

    // The live twin of the leading-WITH guard for DML on Oracle (ADR 0011): the
    // subquery_factoring_clause belongs inside the feeding SELECT.
    [Fact]
    public void LeadingWithBeforeInsert_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "WITH c AS (SELECT 901 AS id, 'x' AS name FROM dual) "
                    + "INSERT INTO users (id, name) SELECT id, name FROM c",
                transaction: transaction));
        transaction.Rollback();
    }

    // SQLA0102 (#569): the live twin for CheckReturningRequiresInto.
    [Fact]
    public void ContextRule_ReturningWithoutInto_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the same INSERT runs, so a rejection below can only
        // come from the INTO-less RETURNING.
        connection.Execute(InsertInto(u, u.Id, u.Name).Values(903, "x"), transaction);

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            InsertInto(u, u.Id, u.Name).Values(904, "x").Returning(u.Id), transaction));
        transaction.Rollback();
    }

    // The continuation Into's ISqlBuilder return forecloses (public-api-design.md
    // § "Opinions…"): an error_logging_clause after RETURNING ... INTO.
    [Fact]
    public void LogErrorsAfterReturningInto_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // DDL commits, so the log table is created outside the rolled-back work.
        connection.Execute("BEGIN DBMS_ERRLOG.CREATE_ERROR_LOG('USERS', 'ERR_USERS'); END;");
        using IDbTransaction transaction = connection.BeginTransaction();
        DynamicParameters outputs = new();
        outputs.Add("outId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        connection.Execute(
            "UPDATE users SET name = name WHERE id = 1 RETURNING id INTO :outId "
                + "LOG ERRORS INTO err_users REJECT LIMIT UNLIMITED",
            outputs,
            transaction);

        Assert.Equal(1, Convert.ToInt32(outputs.Get<object>("outId")!.ToString()));
        transaction.Rollback();
    }

    // The live twin of the leading-WITH guard for MERGE on Oracle (ADR 0011):
    // the merge_statement grammar carries no subquery_factoring_clause.
    [Fact]
    public void LeadingWithBeforeMerge_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the same MERGE with the source inlined runs, so a
        // rejection below can only come from the leading WITH.
        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name FROM dual) s "
                + "ON (t.id = s.id) WHEN MATCHED THEN UPDATE SET t.name = s.name",
            transaction: transaction);

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "WITH c AS (SELECT 1 AS id, 'x' AS name FROM dual) "
                    + "MERGE INTO users t USING c s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // The remedy that guard's message names, proved to run: MERGE's own source
    // slot takes a subquery, so the CTE goes there.
    [Fact]
    public void CteInsideMergeUsingSubquery_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING "
                + "(WITH c AS (SELECT 1 AS id, 'x' AS name FROM dual) SELECT * FROM c) s "
                + "ON (t.id = s.id) WHEN MATCHED THEN UPDATE SET t.name = s.name",
            transaction: transaction);
        transaction.Rollback();
    }

    // ADR 0011 (#523): the accepted twins that keep both constant-sort-key
    // guards off Oracle — it reads either literal as a constant expression.
    [Fact]
    public void OrderByNonIntegerConstant_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 2.5");
    }

    [Fact]
    public void OrderByNegativeOrdinal_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id, name FROM users ORDER BY -1");

        // Zero is still no column position here (ORA-01785), as the
        // dialect-blind guard has it.
        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 0"));
    }

    // SQLA0102's twin (#582): Oracle's grammar has one merge_update_clause and one
    // merge_insert_clause.
    [Fact]
    public void MergeRepeatedWhenBranch_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 id, 'x' name FROM dual) s ON (t.id = s.id) "
                + "WHEN MATCHED THEN UPDATE SET t.name = t.name",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users t USING (SELECT 1 id, 'x' name FROM dual) s ON (t.id = s.id) "
                + "WHEN MATCHED THEN UPDATE SET t.name = t.name "
                + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
            transaction: transaction));

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users t USING (SELECT 999 id, 'x' name FROM dual) s ON (t.id = s.id) "
                + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name) "
                + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name)",
            transaction: transaction));

        transaction.Rollback();
    }

    // #521: Oracle filters a MERGE branch with a trailing WHERE on the action.
    // Only the matched rows passing it are updated (Alice 30, Dave 25).
    [Fact]
    public void Merge_UpdateActionWhere_UpdatesOnlyTheRowsItAdmits()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == "u").Where(t.Age < 35),
            transaction);

        Assert.Equal(2, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c).Where(c.Name == "u"), transaction)));
        transaction.Rollback();
    }

    // The filter may read the source as well as the target (Carol, 50).
    [Fact]
    public void Merge_UpdateActionWhere_ReadsTheSource()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == "u").Where(s.Age > 45),
            transaction);

        Assert.Equal(1, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c).Where(c.Name == "u"), transaction)));
        transaction.Rollback();
    }

    // DELETE WHERE reaches only the rows the WHERE admitted: Bob and Eve go;
    // Carol (50) was never updated, so she stays although she is over 35.
    [Fact]
    public void Merge_UpdateActionWhere_LimitsWhatDeleteWhereSees()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t).Using(s).On(t.Id == s.Id)
                .WhenMatched().ThenUpdateSet(t.Name == s.Name)
                .Where(t.Age < 45).DeleteWhere(t.Age >= 35),
            transaction);

        Assert.Equal(3, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c), transaction)));
        Assert.Equal(1, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c).Where(c.Id == 3), transaction)));
        transaction.Rollback();
    }

    // Why the update's Where comes before DeleteWhere in the chain: the engine takes
    // the WHERE there only.
    [Fact]
    public void MergeDeleteWhereBeforeUpdateActionWhere_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "MERGE INTO users t USING (SELECT id, name FROM users) s ON (t.id = s.id) "
                + "WHEN MATCHED THEN UPDATE SET t.name = s.name DELETE WHERE t.age >= 35 "
                + "WHERE t.age < 45",
            transaction: transaction));
        transaction.Rollback();
    }

    // The insert takes the same trailing WHERE, over the source: of five
    // unmatched rows, those under 35 (Alice, Dave) are inserted.
    [Fact]
    public void Merge_InsertActionWhere_InsertsOnlyTheRowsItAdmits()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Offsetting the key leaves every source row unmatched.
        connection.Execute(
            MergeInto(t).Using(s).On(t.Id == s.Id + 900)
                .WhenNotMatched().ThenInsert(t.Id, t.Name).Values(s.Id + 900, s.Name)
                .Where(s.Age < 35),
            transaction);

        Assert.Equal(2, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c).Where(c.Id > 900), transaction)));
        transaction.Rollback();
    }

    // A matched row the update's filter turns away is not inserted instead:
    // three rows are filtered out, and only the unmatched 901 is inserted.
    [Fact]
    public void Merge_BothBranchesFiltered_ExecutesEachFilterOnItsOwnBranch()
    {
        UsersTable t = new("t");
        UsersTable src = new("src");
        UsersTable c = new();
        SubqueryDerivedTable s =
            Select(src.Id, src.Name, src.Age).From(src)
                .UnionAll.Select(901, "a", 20).From(Dual)
                .AsTable("s");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MergeInto(t).Using(s).On(t.Id == s.Column("id"))
                .WhenMatched().ThenUpdateSet(t.Name == "u").Where(t.Age < 35)
                .WhenNotMatched().ThenInsert(t.Id, t.Name)
                .Values(s.Column("id"), s.Column("name"))
                .Where(s.Column("age") < 35),
            transaction);

        Assert.Equal(2, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c).Where(c.Name == "u"), transaction)));
        Assert.Equal(6, Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(c.Id)).From(c), transaction)));
        transaction.Rollback();
    }

    // An unmatched row has no target row, so the insert filter cannot name
    // one — the claim the insert's Where remarks make.
    [Fact]
    public void MergeInsertActionWhere_NamingTheTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "MERGE INTO users t USING (SELECT 901 id, 'a' name FROM dual) s ON (t.id = s.id) "
                + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name) WHERE t.age > 0",
            transaction: transaction));
        transaction.Rollback();
    }

    // #521 item 2, corrected by #684: Oracle groups by the constant itself, so an
    // aggregate-only list gets one group and a listed column is not grouped by it.
    [Theory]
    [InlineData("1")]
    [InlineData("2.5")]
    [InlineData("0")]
    [InlineData("-1")]
    public void GroupByConstant_GroupsByTheConstant(string constant)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<long>("SELECT COUNT(*) FROM users"),
            connection.Query<long>($"SELECT COUNT(*) FROM users GROUP BY {constant}").Single());
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            $"SELECT department_id FROM users GROUP BY {constant}"));
    }

    // ADR 0012 non-goals (#523): Oracle takes a negative row count outright,
    // which is what keeps the FETCH family unguarded and, for #529, what keeps
    // SQLA0104 silent on both its FETCH spellings; the LAG offset it rejects.
    [Fact]
    public void NegativeFetchCount_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id FROM users ORDER BY id FETCH FIRST -1 ROWS ONLY");
        connection.ExecuteScalar(
            "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY");
        connection.ExecuteScalar("SELECT id FROM users ORDER BY id OFFSET -1 ROWS");
    }

    [Fact]
    public void LagNegativeOffset_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // ORA-01428 is raised as the row is produced, so the probe must fetch
        // one — ExecuteNonQuery on a SELECT never evaluates the window.
        connection.ExecuteScalar("SELECT LAG(id, 1) OVER (ORDER BY id) FROM users");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"));
    }

    // ADR 0012 non-goal (#523 item 1): every letter SqlArtisan can emit is
    // valid here, so no match parameter is universally invalid.
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
    [InlineData("u")] // MySQL's Unicode letter, which Oracle's grammar has not.
    [InlineData("z")]
    public void RegexpMatchParameter_UnknownLetter_IsRejectedByTheEngine(string flags)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(MatchParameterProbe(flags)));
    }

    // 'Ab' matches pattern 'ab' only case-insensitively, so the answer names
    // which half of the contradictory pair the engine applied: the last one.
    [Theory]
    [InlineData("ci", "YES")]
    [InlineData("ic", "NO")]
    public void RegexpContradictoryMatchParameter_ResolvesToTheLastLetter(
        string flags, string expected)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(expected, connection.ExecuteScalar<string>(MatchParameterProbe(flags)));
    }

    private static string MatchParameterProbe(string flags) =>
        $"SELECT CASE WHEN REGEXP_LIKE('Ab', 'ab', '{flags}') THEN 'YES' ELSE 'NO' END FROM dual";

    // RegexpOptions.NewLine's documented meaning (#614): 'n' lets `.` match a
    // newline, which it does not by default.
    [Theory]
    [InlineData(", 'n'", "YES")]
    [InlineData("", "NO")]
    public void RegexpNewLineLetter_LetsDotMatchANewline(string parameter, string expected)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(expected, connection.ExecuteScalar<string>(
            "SELECT CASE WHEN REGEXP_LIKE('a' || CHR(10) || 'b', '^a.b$'"
                + parameter + ") THEN 'YES' ELSE 'NO' END FROM dual"));
    }

    // RegexpOptions.None emits '', which Oracle reads as NULL: the call must still
    // match as with no parameter, not yield NULL (a match only YES can show).
    [Fact]
    public void RegexpEmptyMatchParameter_ReadsAsTheDefault()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal("YES", connection.ExecuteScalar<string>(MatchParameterProbe("").Replace(
            "'ab'", "'Ab'", StringComparison.Ordinal)));
    }

    // docs/query-statements.md's hint example (#614): the alias renders quoted ("u"), so a
    // hint naming it bare folds to U and resolves to nothing, which Oracle drops silently.
    [Fact]
    public void HintNamingAQuotedAliasBare_IsUnresolved()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        if (connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM user_indexes WHERE index_name = 'USERS_IX'") == 0)
        {
            connection.Execute("CREATE INDEX users_ix ON users(name)");
        }

        string bare = HintReport(connection, "/*+ INDEX(u users_ix) */");
        string quoted = HintReport(connection, "/*+ INDEX(\"u\" users_ix) */");

        Assert.True(
            bare.Contains("Unresolved", StringComparison.Ordinal)
                && !quoted.Contains("Unresolved", StringComparison.Ordinal),
            $"bare:\n{bare}\nquoted:\n{quoted}");
    }

    private static string HintReport(IDbConnection connection, string hint)
    {
        connection.Execute($"EXPLAIN PLAN FOR SELECT {hint} \"u\".id FROM users \"u\"");
        return string.Join("\n", connection.Query<string>(
            "SELECT plan_table_output FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, NULL, "
                + "'BASIC +HINT_REPORT'))"));
    }

    // The live twin of SortOrder's repeated-NULLS guard (#614); one ordering runs.
    [Fact]
    public void RepeatedNullOrdering_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id FROM users ORDER BY id NULLS FIRST");

        Assert.ThrowsAny<DbException>(() =>
            connection.ExecuteScalar("SELECT id FROM users ORDER BY id NULLS FIRST NULLS LAST"));
    }

    // #523: SQLA0102's live proofs for the DML-context rules. Oracle is the only
    // engine that rejects all five shapes; the acceptance twin of each is the
    // dialect the spelling belongs to (MySQL's or SQL Server's per-engine tests).
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

    // #679: the lead that does not re-list the target, which Oracle 23 runs, is
    // rejected here too, so SQLA0102 reports either lead below its floor.
    [Fact]
    public void ContextRule_JoinedDeleteFromNotRelisted_Rejected()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            DeleteFrom(o).From(u).Where((o.UserId == u.Id) & (u.Id == 3)), transaction));
        transaction.Rollback();
    }

    [Fact]
    public void ContextRule_DeleteUsing_Rejected()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            DeleteFrom(u).Using(o).Where((u.Id == o.UserId) & (u.Id == 3)), transaction));
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

    // SQL Server's re-listed target leads with the bare alias; Oracle 21c has no
    // UPDATE ... FROM at all (#582).
    [Fact]
    public void JoinedUpdateRelistedTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "UPDATE \"u\" SET age = 1 FROM users \"u\" "
                    + "INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void ContextRule_JoinedUpdateFromForm_Rejected()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            Update(u).Set(u.Age == 999).From(o).Where((u.Id == o.UserId) & (u.Id == 3)),
            transaction));
        transaction.Rollback();
    }

    [Fact] // ORA-01786. The ungrouped lock is proven by the dialect sweep's
           // ForUpdate case, so the GROUP BY is the only difference.
    public void ContextRule_ForUpdateAfterGroupBy_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId)
                .OrderBy(u.DepartmentId).ForUpdate()));
    }

    [Fact] // ORA-02014: the row_limiting_clause cannot be specified with the
           // for_update_clause. The unlocked FETCH and the unlimited lock each run
           // on this lane, so the pairing is the only difference (#520).
    public void ContextRule_ForUpdateAfterRowLimiting_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id).FetchFirst(1).ForUpdate()));
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).ForUpdate()));
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id).OffsetRows(1).FetchNext(1).ForUpdate()));
    }

    // #521: Oracle's OF names a column, and locks the rows of its table alone.
    [Fact]
    public void ForUpdate_OfColumn_LocksOnlyThatColumnsTable()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection a = _fixture.OpenConnection();
        using IDbTransaction ta = a.BeginTransaction();
        a.Query<int>(
            Select(u.Id).From(u).InnerJoin(o).On(o.UserId == u.Id).Where(u.Id == 1)
                .ForUpdate(Of(u.Id)),
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

    // #742 probe: which branch's rows a FOR UPDATE after a UNION locks, if it runs.
    [Fact]
    public void ForUpdateOverUnion_Probe()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection a = _fixture.OpenConnection();
        using IDbTransaction ta = a.BeginTransaction();
        string outcome;
        try
        {
            IEnumerable<int> ids = a.Query<int>(
                Select(u.Id).From(u).Where(u.Id == 1)
                .Union
                .Select(o.Id).From(o).Where(o.Id == 1)
                .ForUpdate(),
                ta);
            outcome = $"ran, rows [{string.Join(",", ids)}]; users locked: "
                + $"{IsRowLocked("users")}; orders locked: {IsRowLocked("orders")}";
        }
        catch (DbException ex)
        {
            outcome = $"rejected: {ex.Message}";
        }

        ta.Rollback();
        Assert.Fail(outcome);
    }

    private bool IsRowLocked(string table)
    {
        using IDbConnection b = _fixture.OpenConnection();
        using IDbTransaction tb = b.BeginTransaction();
        try
        {
            b.Query<int>($"SELECT id FROM {table} WHERE id = 1 FOR UPDATE NOWAIT", transaction: tb)
                .ToList();
            return false;
        }
        catch (DbException)
        {
            return true;
        }
        finally
        {
            tb.Rollback();
        }
    }

    // A string .As("n") renders quoted ("n") while Column("n") renders bare,
    // which Oracle folds to N: the reference misses. Column(alias) matches.
    [Fact]
    public void DerivedColumn_StringAliasReadByName_IsRejected_ReadByAlias_Executes()
    {
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();

        ExpressionAlias n = u.Id.As("n");
        SubqueryDerivedTable s = Select(n).From(u).AsTable("s");

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(s.Column("n")).From(s).Where(s.Column("n") == 1)).ToList());

        Assert.Equal(
            new[] { 1 },
            connection.Query<int>(Select(s.Column(n)).From(s).Where(s.Column(n) == 1)));
    }

    // Oracle folds the bare name to upper case, so by name misses whenever the
    // alias has a lower-case letter, and matches an all-upper-case one.
    [Fact]
    public void DerivedColumn_StringAliasReadByName_MatchesOnlyWhenUpperCase()
    {
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();

        SubqueryDerivedTable mixed = Select(u.Id.As("Nn")).From(u).AsTable("s");
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(mixed.Column("Nn")).From(mixed).Where(mixed.Column("Nn") == 1)).ToList());

        SubqueryDerivedTable upper = Select(u.Id.As("N")).From(u).AsTable("s");
        Assert.Equal(
            new[] { 1 },
            connection.Query<int>(
                Select(upper.Column("N")).From(upper).Where(upper.Column("N") == 1)));
    }

    // A CTE carries the same pairing: its body's string alias renders quoted.
    [Fact]
    public void CteColumn_StringAliasReadByName_IsRejected_ReadByAlias_Executes()
    {
        UsersTable u = new("u");
        Cte c = new("c");
        ExpressionAlias n = u.Id.As("n");
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            With(c.As(Select(n).From(u)))
                .Select(c.Column("n")).From(c).Where(c.Column("n") == 1)).ToList());

        Assert.Equal(
            new[] { 1 },
            connection.Query<int>(
                With(c.As(Select(n).From(u)))
                    .Select(c.Column(n)).From(c).Where(c.Column(n) == 1)));
    }

    // A DerivedTable handle carries the same pairing as a subquery source.
    [Fact]
    public void DerivedTableColumn_StringAliasReadByName_IsRejected_ReadByAlias_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        DerivedTable x = new("x");
        ExpressionAlias n = o.Id.As("n");
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(x.Column("n")).From(u)
                .CrossApply(Select(n).From(o).Where(o.UserId == u.Id), x)).ToList());

        Assert.NotEmpty(connection.Query<int>(
            Select(x.Column(n)).From(u)
                .CrossApply(Select(n).From(o).Where(o.UserId == u.Id), x)));
    }

    // #590: Oracle XE 21.3.0 takes FOR UPDATE only in a top-level SELECT; each unlocked control
    // runs, so the lock is what the engine rejects.
    [Fact]
    public void LockedSubqueryInIn_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders)").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedExistsSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id)").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id = (SELECT user_id FROM orders WHERE id = 1)").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id = "
            + "(SELECT user_id FROM orders WHERE id = 1 FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedSelectedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id, (SELECT o.id FROM users o WHERE o.id = u.id) FROM users u").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT u.id, (SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) "
            + "FROM users u").ToList());
    }

    [Fact]
    public void LockedCrossApply_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id FROM users u "
            + "CROSS APPLY (SELECT o.id FROM users o WHERE o.id = u.id) x").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT u.id FROM users u "
            + "CROSS APPLY (SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    // #675: a source-carrying derived table emits its source twice, which the engine is
    // left to reject (guards-and-empty-states.md); the DerivedTable handle runs.
    [Fact]
    public void CrossApply_SourceCarryingDerivedTable_IsRejectedByTheEngine()
    {
        UsersTable u = new("u");
        UsersTable s = new("s");
        using IDbConnection connection = _fixture.OpenConnection();
        ISubquery sub = Select(s.Id).From(s).Where(s.Id == u.Id);

        connection.Query<int>(
            Select(u.Id).From(u).CrossApply(sub, new DerivedTable("z"))).ToList();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u).CrossApply(sub, sub.AsTable("z"))).ToList());
    }

    [Fact]
    public void LockedLateralInlineView_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id) x").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    [Fact]
    public void LockedCteBody_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "WITH c AS (SELECT id FROM users) SELECT id FROM c").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "WITH c AS (SELECT id FROM users FOR UPDATE) SELECT id FROM c").ToList());
    }

    [Fact]
    public void LockedDerivedTable_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users) d").ToList();
        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users FOR UPDATE) d").ToList());
    }

    // #662: whether the engine runs a window on a DISTINCT aggregate decides
    // whether the library may reject the pairing as valid on no dialect (ADR 0007).
    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.OrderedOnly),
        MemberType = typeof(DistinctAggregateWindows))]
    public void DistinctAggregateOrderedWindow_IsRejectedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(sql));
    }

    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.UnorderedOnly),
        MemberType = typeof(DistinctAggregateWindows))]
    public void DistinctAggregateUnorderedWindow_IsAcceptedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute(sql);
    }

    // #666: ANY is half of a comparison, valid nowhere on its own (Oracle XE 21.3.0).
    [Fact]
    public void QuantifiedOperand_OutsideAComparison_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT CASE WHEN 1 = ANY (SELECT 1 FROM dual) THEN 1 END FROM dual"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT ANY (SELECT 1 FROM dual) FROM dual"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT CASE WHEN 'a' LIKE ANY (SELECT 'a' FROM dual) ESCAPE '!' THEN 1 END "
                + "FROM dual"));
    }

    // docs/analyzer.md (SQLA0205): Oracle converts the text side to a number, so a
    // zero-padded text matches and a non-numeric one fails the query.
    [Fact]
    public void TextComparedToNumber_ConvertsTheText()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        const string Numeric =
            "SELECT '01500001' z FROM DUAL UNION ALL SELECT '1500001' FROM DUAL";

        Assert.Equal(2, connection.ExecuteScalar<int>(
            $"SELECT COUNT(*) FROM ({Numeric}) t WHERE z = :n", new { n = 1500001 }));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar<int>(
            $"SELECT COUNT(*) FROM ({Numeric} UNION ALL SELECT 'abc' FROM DUAL) t WHERE z = :n",
            new { n = 1500001 }));
    }

    // README Manual Execution: ODP.NET binds by position unless BindByName is set, and a
    // statement repeating a marker ahead of another then does not run (on this lane it
    // waits out the command timeout).
    [Fact]
    public void RepeatedMarkerAheadOfAnother_BindByPosition_DoesNotRun()
    {
        UsersTable u = new();
        BindValue id = Bind(1);
        SqlStatement statement =
            Select(u.Name).From(u).Where(u.Id == id | u.Id > id | u.Name == "x").Build(Dbms.Oracle);
        object? Run(bool byName)
        {
            using IDbConnection connection = _fixture.OpenConnection();
            using OracleCommand command = ((OracleConnection)connection).CreateCommand();
            command.BindByName = byName;
            command.CommandTimeout = 5;
            command.CommandText = statement.Text;
            statement.Parameters.ForEach((name, value) => command.Parameters.Add(
                new OracleParameter(name.TrimStart(':'), value.Value)));
            return command.ExecuteScalar();
        }

        Run(byName: true);
        Task<object?> byPosition = Task.Run(() => Run(byName: false));
        Assert.True(
            ((IAsyncResult)byPosition).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(60)),
            "Binding by position did not finish within 60 seconds.");
        Assert.IsType<OracleException>(byPosition.Exception?.InnerException);
    }

    // docs/functions.md: the RegexpReplace warning; occurrence 0 replaces every match.
    [Fact]
    public void RegexpReplace_ThreeArguments_ReplacesEveryMatch()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0")).From(Dual)));
        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0", 1)).From(Dual)));
        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0", 1, 0)).From(Dual)));
    }

    // docs/functions.md: the Length warning.
    [Fact]
    public void Length_NonAsciiCharacter_CountsCharacters()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            1, Convert.ToInt32(connection.ExecuteScalar(Select(Length("\u00e9")).From(Dual))));
    }

    // docs/functions.md: GREATEST and LEAST return NULL for a NULL argument here.
    [Fact]
    public void GreatestAndLeast_NullArgument_ReturnsNull()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Null(connection.ExecuteScalar<int?>(Select(Greatest(Null, 3, 1)).From(Dual)));
        Assert.Null(connection.ExecuteScalar<int?>(Select(Least(Null, 3, 1)).From(Dual)));
    }

    // docs/query-statements.md: set operators apply left to right here, so a
    // derived table is what groups the INTERSECT first.
    [Fact]
    public void SetOperators_IntersectAfterUnionOrExcept_AppliesLeftToRight()
    {
        UsersTable a = new("a");
        UsersTable b = new("b");
        UsersTable c = new("c");
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection.Query<int>(
            Select(a.Id).From(a).Where(a.Id == 1)
            .Union
            .Select(b.Id).From(b).Where(b.Id == 2)
            .Intersect
            .Select(c.Id).From(c).Where(c.Id == 3));
        SubqueryDerivedTable x =
            Select(b.Id).From(b).Where(b.Id == 2)
            .Intersect
            .Select(c.Id).From(c).Where(c.Id == 3)
            .AsTable("x");
        IEnumerable<int> grouped = connection.Query<int>(
            Select(a.Id).From(a).Where(a.Id == 1).Union.Select(x.Column(b.Id)).From(x));
        IEnumerable<int> exceptIds = connection.Query<int>(
            Select(a.Id).From(a).Where(a.Id == 1)
            .Except
            .Select(b.Id).From(b).Where(b.Id == 1)
            .Intersect
            .Select(c.Id).From(c).Where(c.Id == 2));
        SubqueryDerivedTable y =
            Select(b.Id).From(b).Where(b.Id == 1)
            .Intersect
            .Select(c.Id).From(c).Where(c.Id == 2)
            .AsTable("y");
        IEnumerable<int> exceptGrouped = connection.Query<int>(
            Select(a.Id).From(a).Where(a.Id == 1).Except.Select(y.Column(b.Id)).From(y));

        Assert.Empty(ids);
        Assert.Equal(new[] { 1 }, grouped);
        Assert.Empty(exceptIds);
        Assert.Equal(new[] { 1 }, exceptGrouped);
    }

    // docs/expressions.md: a bound backslash is an ordinary pattern character here.
    [Fact]
    public void Like_BoundBackslash_MatchesItself()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long matched = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(Bind(@"a\b").Like(@"a\b"))));

        Assert.Equal(5, matched);
    }

    // docs/cookbook.md: Oracle takes a column listed twice in ORDER BY.
    [Fact]
    public void OrderBy_SameColumnTwice_IsAcceptedByTheEngine()
    {
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();

        IEnumerable<int> ids = connection.Query<int>(
            Select(u.Id).From(u).OrderBy(u.Id.Desc, u.Id).OffsetRows(0).FetchNext(2));

        Assert.Equal(new[] { 5, 4 }, ids);
    }

    // #669: an empty and a blank interval value are both rejected here.
    [Fact]
    public void IntervalLiteral_EmptyOrBlankValue_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT INTERVAL '1' DAY FROM DUAL");

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT INTERVAL '' DAY FROM DUAL"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT INTERVAL ' ' DAY FROM DUAL"));
    }

    // #669: the twin of SQLite's In_EmptyList_IsAcceptedByTheEngine.
    [Fact]
    public void In_EmptyList_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar<int>("SELECT COUNT(*) FROM users WHERE id IN (1)");

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM users WHERE id IN ()"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM users WHERE id NOT IN ()"));
    }

    // #669: RegexpOptions.None emits an empty match parameter, which Oracle reads as
    // NULL; it must filter as the two-argument form does.
    [Fact]
    public void RegexpLike_EmptyMatchParameter_FiltersAsTheTwoArgumentForm()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        int twoArguments = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM users WHERE REGEXP_LIKE(name, 'A')");
        int emptyMatchParameter = connection.ExecuteScalar<int>(
            Select(Count(Asterisk)).From(u).Where(RegexpLike(u.Name, "A", RegexpOptions.None)));

        Assert.True(twoArguments > 0);
        Assert.Equal(twoArguments, emptyMatchParameter);
    }

    // #742: Oracle converts a string beside `+` to a number, so beside a name it fails;
    // DoublePipe concatenates the same operands.
    [Fact]
    public void StringPlusTextColumn_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal("Dr. Alice", connection.ExecuteScalar<string>(
            Select(DoublePipe("Dr. ", u.Name)).From(u).Where(u.Id == 1)));

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select("Dr. " + u.Name).From(u).Where(u.Id == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(u.Name + " ").From(u).Where(u.Id == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(u.Name + ' ').From(u).Where(u.Id == 1)));
    }

    // #742: a numeric string beside `+` adds as a number here, as on MySQL and SQLite.
    [Fact]
    public void NumericStringPlusNumberColumn_AddsNumerically()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(31, connection.ExecuteScalar<int>(
            Select("1" + u.Age).From(u).Where(u.Id == 1)));
    }

    // #742: two text columns under `+` fail here, as a string beside one does
    // (StringPlusTextColumn_IsRejectedByTheEngine).
    [Fact]
    public void TextColumnPlusTextColumn_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(u.Name + u.Name).From(u).Where(u.Id == 1)));
    }

    // #742: an untyped BindNull() binds as text, so a simple CASE over a number rejects it
    // (ORA-00932) where Sql.Null skips the branch; SQLA0303 reads Sql.Null alone there.
    [Fact]
    public void SimpleCaseWhenUntypedBindNull_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(0, connection.ExecuteScalar<int>(
            Select(Count(Asterisk))
            .From(u)
            .Where(Case(u.Id, When(Null).Then(1), Else(0)) == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(Count(Asterisk))
            .From(u)
            .Where(Case(u.Id, When(BindNull()).Then(1), Else(0)) == 1)));
    }
}
