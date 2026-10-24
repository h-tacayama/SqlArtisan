using System.Data;
using System.Data.Common;
using Dapper;
using MySqlConnector;
using SqlArtisan;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "MySql")]
public sealed class MySqlTests : IntegrationTestBase, IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlTests(MySqlFixture fixture) : base(fixture)
    {
        _fixture = fixture;
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
    public void Upsert_OnDuplicateKeyUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Values(1, "AliceUpdated")
                .OnDuplicateKeyUpdate(u.Name == Excluded(u.Name)),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("AliceUpdated", name);
        transaction.Rollback();
    }

    [Fact]
    public void StringAggregation_GroupConcat_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name)).From(u))
            .Single();

        Assert.Contains("Alice", concatenated);
    }

    [Fact]
    public void StringAggregation_GroupConcatSeparator_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // GROUP_CONCAT(name SEPARATOR ' | ') — the SEPARATOR clause is inlined.
        string concatenated = connection
            .Query<string>(Select(GroupConcat(u.Name, Separator(" | "))).From(u))
            .Single();

        Assert.Contains(" | ", concatenated);
    }

    [Fact]
    public void JsonExtract_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // MySQL returns a JSON scalar as a quoted string, so the value is asserted
        // with Contains rather than an exact match.
        string name = connection
            .Query<string>(Select(JsonExtract(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("Alice", name);
    }

    [Fact]
    public void JsonArrowText_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data ->> '$.name') — the key is inline; ->> returns the unquoted scalar.
        string name = connection
            .Query<string>(Select(JsonArrowText(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    // MySQL's grammar takes only a string literal right of ->, so the inline key
    // runs under a server-side prepared statement, where a bound one fails (#557).
    [Fact]
    public void JsonArrow_InlineKey_RunsServerPrepared()
    {
        UsersTable u = new();

        object? name = ExecuteServerPrepared(
            Select(JsonArrowText(u.Data, "$.name")).From(u).Where(u.Id == 1));

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonArrow_BoundKey_IsRejectedServerPrepared()
    {
        UsersTable u = new();

        MySqlException ex = Assert.Throws<MySqlException>(() => ExecuteServerPrepared(
            Select(JsonArrowText(u.Data, Bind("$.name"))).From(u).Where(u.Id == 1)));
        Assert.Equal(MySqlErrorCode.ParseError, ex.ErrorCode);
    }

    // ADR 0016 (#661): a bound DATE_FORMAT format survives a server-side prepare, so
    // MySQL's grammar gives no reason to inline it, as it did the ->> key above.
    [Fact]
    public void DateFormat_BoundFormat_RunsServerPrepared()
    {
        UsersTable u = new();

        object? formatted = ExecuteServerPrepared(
            Select(DateFormat(u.CreatedAt, "%Y-%m")).From(u).Where(u.Id == 1));

        Assert.Equal("2020-03", formatted);
    }

    private object? ExecuteServerPrepared(ISqlBuilder builder)
    {
        SqlStatement statement = builder.Build(Dbms.MySql);
        using MySqlConnection connection = new(_fixture.ConnectionString + ";IgnorePrepare=false");
        connection.Open();
        using MySqlCommand command = new(statement.Text, connection);
        statement.Parameters.ForEach(
            (name, value) => command.Parameters.AddWithValue(name, value.Value));
        command.Prepare();
        return command.ExecuteScalar();
    }

    [Fact]
    public void JsonArrow_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // (data -> '$.address') returns the nested JSON object.
        string address = connection
            .Query<string>(Select(JsonArrow(u.Data, "$.address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact] // #255 / #239 (ERG-09): MySQL accepts an aliased single-table DELETE
           // target as of 8.0.16 — the safe spelling for a correlated DELETE here.
    public void DeleteAliasedTarget_Executes()
    {
        UsersTable cu = new("cu");
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name, u.Age, u.DepartmentId).Values(300, "Temp", 20, 99),
            transaction);
        connection.Execute(DeleteFrom(cu).Where(cu.Id == 300), transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == 300), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }

    [Fact] // ADR 0011: MySQL's INSERT grammar has no target-alias slot (the 8.0.19+
           // AS row_alias is post-VALUES), so Build(MySql) throws for an aliased
           // target; anchored live here beside the unaliased form succeeding.
    public void AliasedInsertTarget_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        using (IDbTransaction tx = connection.BeginTransaction())
        {
            connection.Execute(
                "INSERT INTO users (id, name, age, department_id) VALUES (901, 'x', 20, 99)",
                transaction: tx);
            tx.Rollback();
        }

        // Inside a rolled-back transaction: if the grammar assumption ever
        // failed, the row must not leak into the shared fixture.
        using (IDbTransaction probeTx = connection.BeginTransaction())
        {
            Assert.ThrowsAny<Exception>(() => connection.Execute(
                "INSERT INTO users AS `cu` (id, name, age, department_id) VALUES "
                    + "(901, 'x', 20, 99)",
                transaction: probeTx));
            probeTx.Rollback();
        }
    }

    [Fact]
    public void JoinedUpdateJoin_Executes()
    {
        // MySQL's multi-table UPDATE joins before SET.
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).InnerJoin(o).On(u.Id == o.UserId).Set(u.Age == 999).Where(u.Id == 3),
            transaction);

        int age = connection
            .Query<int>(Select(read.Age).From(read).Where(read.Id == 3), transaction)
            .Single();

        Assert.Equal(999, age);
        transaction.Rollback();
    }

    [Fact]
    public void JoinedDeleteFrom_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            DeleteFrom(u).From(u).InnerJoin(o).On(u.Id == o.UserId).Where(u.Id == 3),
            transaction);

        long remaining = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(read.Id)).From(read).Where(read.Id == 3), transaction));

        Assert.Equal(0, remaining);
        transaction.Rollback();
    }

    [Fact] // #264: SQLA0102's live proof. Top-level LIMIT acceptance is proven by
           // Pagination_LimitOffset_Executes; the position is the only difference.
    public void ContextRule_LimitInInSubquery_Rejected()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u)
                .Where(u.Id.In(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)))));
    }

    [Fact] // SQLA0102 fires on NotIn too; MySQL's LIMIT restriction covers NOT IN
           // as well as IN/ALL/ANY/SOME, so the NOT IN arm is live-proven here.
    public void ContextRule_LimitInNotInSubquery_Rejected()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(u.Id).From(u)
                .Where(u.Id.NotIn(Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2)))));
    }

    // The acceptance half of the LIMIT-in-IN notes: the workaround they name, a CTE or a
    // derived table holding the limited query, runs under the same IN.
    [Fact]
    public void ContextRule_LimitThroughCteOrDerivedTable_Executes()
    {
        UsersTable u = new();
        OrdersTable o = new("o");
        Cte c = new("c");
        using IDbConnection connection = _fixture.OpenConnection();
        ISelectBuilderLimitOffset Limited() =>
            Select(o.UserId).From(o).OrderBy(o.UserId).Limit(2);
        int[] expected = [.. connection.Query<int>(Limited()).Distinct().Order()];
        SubqueryDerivedTable d = Limited().AsTable("d");

        IEnumerable<int> viaCte = connection.Query<int>(
            With(c.As(Select(o.UserId.As(c.Column("uid"))).From(o).OrderBy(o.UserId).Limit(2)))
            .Select(u.Id)
            .From(u)
            .Where(u.Id.In(Select(c.Column("uid")).From(c)))
            .OrderBy(u.Id));
        IEnumerable<int> viaDerived = connection.Query<int>(
            Select(u.Id).From(u).Where(u.Id.In(Select(d.Column(o.UserId)).From(d))).OrderBy(u.Id));

        Assert.Equal(expected, viaCte);
        Assert.Equal(expected, viaDerived);
    }

    [Fact] // #264: SQLA0102's live proof. GROUPING() under WITH ROLLUP is proven by
           // the dialect sweep's MySQL branch; the missing suffix is the only difference.
    public void ContextRule_GroupingWithoutWithRollup_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(Grouping(u.DepartmentId)).From(u).GroupBy(u.DepartmentId)));
    }

    [Fact] // #436, #666: the live proof behind SQLA0102's Interval rule and the type that
           // keeps INTERVAL out of a value position — only the arithmetic wrapper differs.
           // MySQL 8.0 also reads the interval-first and string-first + the library emits.
    public void ContextRule_IntervalOutsideArithmetic_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<DateTime>("SELECT created_at + INTERVAL 30 DAY FROM users");
        Assert.ThrowsAny<DbException>(
            () => connection.Query<object>("SELECT INTERVAL 30 DAY FROM users"));

        Assert.Equal(
            connection.Query<DateTime>(
                Select(u.CreatedAt + Interval(30, DateTimePart.Day)).From(u)),
            connection.Query<DateTime>(
                Select(Interval(30, DateTimePart.Day) + u.CreatedAt).From(u)));
        Assert.Equal(
            new DateTime(2024, 1, 31),
            Convert.ToDateTime(connection.ExecuteScalar(
                Select("2024-01-01" + Interval(30, DateTimePart.Day)))));
    }

    // #362: SQLA0205's live proof — MySQL compares a string to a number as floating
    // point, so the mismatch changes which rows come back, not merely how fast.
    [Fact]
    public void TextColumnComparedToNumber_MatchesRowsThatAreNotEqualAsText()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute("CREATE TEMPORARY TABLE zip_probe (code varchar(16))");

        try
        {
            connection.Execute(
                "INSERT INTO zip_probe (code) VALUES ('150'), ('0150'), ('150abc'), ('abc')");

            IEnumerable<string> asNumber = connection.Query<string>(
                "SELECT code FROM zip_probe WHERE code = 150 ORDER BY code");

            IEnumerable<string> asText = connection.Query<string>(
                "SELECT code FROM zip_probe WHERE code = '150' ORDER BY code");

            Assert.Equal(new[] { "0150", "150", "150abc" }, asNumber);
            Assert.Equal(new[] { "150" }, asText);
        }
        finally
        {
            connection.Execute("DROP TEMPORARY TABLE IF EXISTS zip_probe");
        }
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

        // The only difference each time — the value-domain violation — is what MySQL rejects.
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
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED "
                + "PRECEDING) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN UNBOUNDED FOLLOWING AND UNBOUNDED "
                + "FOLLOWING) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors MySqlTemporalUnits in DatepartValidity.cs —
           // EPOCH is a PostgreSQL field, not a MySQL EXTRACT()/DATE_ADD() unit.
    public void Extract_EpochUnit_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed unit is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT EXTRACT(DAY FROM created_at) FROM users");

        // The only difference — the unit EXTRACT() doesn't have on MySQL — is
        // what MySQL rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT EXTRACT(EPOCH FROM created_at) FROM users"));
    }

    // SQLA0103 (#684): an alias at the analyzer's limit comes back whole, and one
    // character past it comes back cut short, with no error.
    [Theory]
    [MemberData(nameof(IdentifierLimits.Characters), MemberType = typeof(IdentifierLimits))]
    public void Alias_PastTheIdentifierLimit_IsTruncated(char character)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string atLimit = IdentifierLimits.AtTheLimit(TargetDbms.MySql, character);

        Assert.Equal(atLimit, IdentifierLimits.ReturnedAlias(connection, atLimit));
        Assert.Equal(atLimit, IdentifierLimits.ReturnedAlias(connection, atLimit + character));
    }

    // SQLA0104 (#684): every member against the four MySQL cells, so a list
    // holding a unit MySQL rejects, or missing one it runs, fails here.
    [Theory]
    [MemberData(nameof(DatepartCells.EveryMember), MemberType = typeof(DatepartCells))]
    public void DatepartCells_EveryMember_MatchTheEngine(DateTimePart part)
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        bool Listed(string member) =>
            DatepartValidity.For(member, TargetDbms.MySql)!.Contains(part.ToString());

        Assert.Equal(
            Listed("Extract"),
            DatepartCells.Runs(connection, Select(Extract(part, u.CreatedAt)).From(u)));
        Assert.Equal(
            Listed("Interval"),
            DatepartCells.Runs(
                connection, Select(DateAdd(u.CreatedAt, Interval(1, part))).From(u)));
        Assert.Equal(
            Listed("Timestampadd"),
            DatepartCells.Runs(
                connection, Select(Timestampadd(part, 1, u.CreatedAt)).From(u)));
        Assert.Equal(
            Listed("Timestampdiff"),
            DatepartCells.Runs(
                connection, Select(Timestampdiff(part, u.CreatedAt, u.CreatedAt)).From(u)));
    }

    // The live twin of SelectBuilder's bare-OFFSET guard (ADR 0011).
    [Fact]
    public void Pagination_OffsetWithoutLimit_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY id OFFSET 1"));
    }

    // #661: an interval literal with no unit, or with a field range, is a syntax error
    // here, the one engine with DATE_SUB, and is left to SQLA0100 rather than guarded.
    [Fact]
    public void DateSub_IntervalLiteralWithoutAUnit_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(
            Select(DateSub(u.CreatedAt, IntervalLiteral("3", Day()))).From(u).Where(u.Id == 1));

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(DateSub(u.CreatedAt, IntervalLiteral("3"))).From(u).Where(u.Id == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(DateSub(u.CreatedAt, IntervalLiteral("1 2", Day(), ToHour)))
            .From(u)
            .Where(u.Id == 1)));
    }

    // ADR 0012 (#661): anchors the non-negative CURRENT_TIMESTAMP / CURRENT_TIME
    // precision guards.
    [Fact]
    public void CurrentTimestamp_NegativePrecision_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT CURRENT_TIMESTAMP(3), CURRENT_TIME(3)");

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT CURRENT_TIMESTAMP(-1)"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT CURRENT_TIME(-1)"));
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
    public void DuplicateSetAssignment_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "UPDATE users SET age = 1, age = 2 WHERE id = 1",
            transaction: transaction);
        int age = connection
            .Query<int>("SELECT age FROM users WHERE id = 1", transaction: transaction)
            .Single();

        Assert.Equal(2, age); // the last assignment wins, silently
        transaction.Rollback();
    }

    [Fact]
    public void DuplicateUsingColumn_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute("SELECT id FROM users JOIN orders USING (id, id)");
    }

    [Fact]
    public void DuplicateCteColumnName_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("WITH x(a, a) AS (SELECT 1, 2) SELECT * FROM x"));
    }

    [Fact]
    public void DuplicateCteName_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("WITH x AS (SELECT 1), x AS (SELECT 2) SELECT * FROM x"));
    }

    // The live twin of the leading-WITH guard for INSERT on MySQL (ADR 0011).
    [Fact]
    public void LeadingWithBeforeInsert_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "WITH c AS (SELECT 901 AS id, 'x' AS name) "
                    + "INSERT INTO users (id, name) SELECT id, name FROM c",
                transaction: transaction));
        transaction.Rollback();
    }

    // MySQL 8.0 has no MERGE at all, so the CTE-fed form is out of reach here for
    // want of the statement — SQLA0100's verdict, and no guard's.
    [Fact]
    public void MergeStatement_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the leading WITH before an UPDATE runs here, so
        // the rejections below are the missing MERGE and nothing else.
        connection.Execute(
            "WITH c AS (SELECT 1 AS id) UPDATE users SET name = name "
                + "WHERE id IN (SELECT id FROM c)",
            transaction: transaction);

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name) s ON t.id = s.id "
                    + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
                transaction: transaction));
        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "WITH c AS (SELECT 1 AS id, 'x' AS name) "
                    + "MERGE INTO users t USING c s ON t.id = s.id "
                    + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // The live twins of the ORDER BY ordinal guards: MySQL rejects position 0
    // but reads a negative literal as a constant and accepts it — the asymmetry
    // that scopes the ADR 0011 arm to PostgreSQL and SQLite.
    [Fact]
    public void OrderByZeroOrdinal_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY 0"));
    }

    [Fact]
    public void OrderByNegativeOrdinal_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>("SELECT id FROM users ORDER BY -1").ToList();
    }

    // #523 item 7: MySQL spells both of these as standalone functions, never as
    // EXTRACT units — the attribution DateTimePart's summaries used to claim.
    [Fact]
    public void WeekdayAndDayofyear_AreNotExtractUnits()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The standalone functions run, so the table and column are right.
        connection.ExecuteScalar("SELECT WEEKDAY(created_at) FROM users");
        connection.ExecuteScalar("SELECT DAYOFYEAR(created_at) FROM users");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT EXTRACT(WEEKDAY FROM created_at) FROM users"));
        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT EXTRACT(DAYOFYEAR FROM created_at) FROM users"));
    }

    // ADR 0012 non-goal (#523): MySQL's window grammar takes an unsigned
    // integer, so the negative offset never reaches execution.
    [Fact]
    public void LagNegativeOffset_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT LAG(id, 1) OVER (ORDER BY id) FROM users");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"));
    }

    // ADR 0012 non-goal (#523): the parse error the negative-row-count entry
    // names. MySQL's LIMIT grammar takes an unsigned integer only.
    [Fact]
    public void NegativeLimitCount_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id FROM users ORDER BY id LIMIT 1");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id FROM users ORDER BY id LIMIT -1"));
    }

    // The MySQL half of #532's per-dialect offset verdict: docs name every
    // engine the behaviour holds on, so this cell has to be measured too.
    [Fact]
    public void NegativeOffset_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id FROM users ORDER BY id LIMIT 10 OFFSET 0");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id FROM users ORDER BY id LIMIT 10 OFFSET -1"));
    }

    // #521 item 2: whether GROUP BY takes an ordinal, and what it does with an
    // out-of-range or fractional one. Probed before the API is widened.
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

    // An aggregate-only list, so a constant read as one group would run (#684).
    [Fact]
    public void GroupByZero_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT COUNT(*) FROM users GROUP BY 0"));
    }

    // Unlike 0, a negative key is no position MySQL checks: one constant group (#684).
    [Fact]
    public void GroupByNegativeConstant_GroupsByTheConstant()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<long>("SELECT COUNT(*) FROM users"),
            connection.Query<long>("SELECT COUNT(*) FROM users GROUP BY -1").Single());
    }

    // Diverges from PostgreSQL, which rejects a non-integer constant outright.
    [Fact]
    public void GroupByNonIntegerConstant_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT COUNT(*) FROM users GROUP BY 2.5");
    }

    // ADR 0011: the acceptance that keeps the non-integer sort-key guard off
    // MySQL — it reads the literal as a constant and orders by nothing.
    [Fact]
    public void OrderByNonIntegerConstant_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 2.5");
    }

    // ADR 0012 non-goal (#523 item 1): MySQL's match_type has no 'x', which is
    // why RegexpOptions.ExcludingWhiteSpace has no spelling here.
    [Theory]
    [InlineData("")]
    [InlineData("c")]
    [InlineData("i")]
    [InlineData("m")]
    [InlineData("n")]
    [InlineData("u")]
    public void RegexpMatchParameter_IsAcceptedByTheEngine(string flags)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(MatchParameterProbe(flags));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("z")]
    public void RegexpMatchParameter_UnknownLetter_IsRejectedByTheEngine(string flags)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(MatchParameterProbe(flags)));
    }

    [Theory]
    [InlineData("ci", 1)]
    [InlineData("ic", 0)]
    public void RegexpContradictoryMatchParameter_ResolvesToTheLastLetter(
        string flags, int expected)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(expected, connection.ExecuteScalar<int>(MatchParameterProbe(flags)));
    }

    private static string MatchParameterProbe(string flags) =>
        $"SELECT REGEXP_LIKE('Ab', 'ab', '{flags}')";

    // Why Against(...)'s null message names no Sql.Null remedy (#614): AGAINST (NULL)
    // runs and matches no row in every modifier form, beside a twin that matches Alice.
    [Theory]
    [InlineData("")]
    [InlineData(" IN NATURAL LANGUAGE MODE")]
    [InlineData(" IN BOOLEAN MODE")]
    [InlineData(" WITH QUERY EXPANSION")]
    public void AgainstNull_MatchesNoRow(string modifier)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        long existing = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM information_schema.STATISTICS "
            + "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' "
            + "AND INDEX_NAME = 'probe_ft_name'");
        if (existing == 0)
        {
            connection.Execute("CREATE FULLTEXT INDEX probe_ft_name ON users(name)");
        }

        Assert.True(connection.ExecuteScalar<long>(
            $"SELECT COUNT(*) FROM users WHERE MATCH(name) AGAINST('Alice'{modifier})") > 0);

        string verdict;
        try
        {
            verdict = "rows=" + connection.ExecuteScalar<long>(
                $"SELECT COUNT(*) FROM users WHERE MATCH(name) AGAINST(NULL{modifier})");
        }
        catch (DbException ex)
        {
            verdict = "rejected: " + ex.Message;
        }

        Assert.Equal("rows=0", verdict);
    }

    // RegexpOptions.NewLine's documented meaning (#614): 'n' lets `.` match a
    // line terminator, which it does not by default. A '\n' escape, not CHAR(10),
    // keeps the subject non-binary, which REGEXP_LIKE requires beside the pattern.
    [Theory]
    [InlineData("n", 1)]
    [InlineData("", 0)]
    public void RegexpNewLineLetter_LetsDotMatchANewline(string flags, int expected)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(expected, connection.ExecuteScalar<int>(
            $"SELECT REGEXP_LIKE('a\\nb', '^a.b$', '{flags}')"));
    }

    // SQLA0104 reads the alphabet per dialect, not per function (#528), so the
    // 'x' gap is pinned on every match_type taker — each paired with a letter
    // MySQL does have, so a rejection cannot come from the call shape instead.
    [Theory]
    [InlineData("SELECT REGEXP_INSTR('Ab', 'ab', 1, 1, 0, '@')")]
    [InlineData("SELECT REGEXP_REPLACE('Ab', 'ab', 'x', 1, 0, '@')")]
    [InlineData("SELECT REGEXP_SUBSTR('Ab', 'ab', 1, 1, '@')")]
    public void RegexpMatchParameter_ExcludingWhiteSpace_IsRejectedByTheEngine(string probe)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar(probe.Replace("@", "i"));

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(probe.Replace("@", "x")));
    }

    // SQL Server's re-listed target leads with the bare alias; MySQL has no
    // UPDATE ... FROM at all (#582).
    [Fact]
    public void JoinedUpdateRelistedTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "UPDATE `u` SET age = 1 FROM users AS `u` "
                    + "INNER JOIN orders AS `o` ON `o`.user_id = `u`.id",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact] // #523: SQLA0102's live proof. MySQL has no UPDATE ... FROM; the JOIN
           // spelling it does own is proven by JoinedUpdateJoin_Executes above.
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

    // #523: why MySQL is absent from the FOR UPDATE context rule — it locks the
    // grouped query's base rows where Oracle and PostgreSQL reject the statement.
    [Fact]
    public void GroupedForUpdate_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> departments = connection.Query<int>(
            Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId)
                .OrderBy(u.DepartmentId).ForUpdate(),
            transaction);

        Assert.Equal(new[] { 10, 20, 30 }, departments);
        transaction.Rollback();
    }

    // A continuation not offered after ForUpdate: INTO after the lock,
    // the position the manual prefers from 8.0.20. SqlArtisan has no SELECT ... INTO.
    [Fact]
    public void IntoAfterLock_IsAcceptedByTheEngine()
    {
        // Without AllowUserVariables the driver reads @id as a missing parameter.
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString)
        {
            AllowUserVariables = true,
        };
        using MySqlConnection connection = new(builder.ConnectionString);
        connection.Open();
        using MySqlTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "SELECT id FROM users WHERE id = 1 FOR UPDATE INTO @id", transaction: transaction);

        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT @id", transaction: transaction));
        transaction.Rollback();
    }

    // #520: why only row-limit-then-lock is offered. The reverse order is a parse
    // error here, and the fluent chain does not offer it (a held stage can still write
    // it, SD23), so the twin is raw SQL; the accepted order is LimitedForUpdate_Executes.
    [Fact]
    public void ForUpdateBeforeLimit_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            "SELECT id FROM users ORDER BY id FOR UPDATE LIMIT 1"));
    }

    // #520: the queue-worker claim — take the first unlocked row and lock it.
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

    // #521: MySQL's OF names the relation to lock; the joined one stays free.
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

    // #521: an unaliased schema-qualified table locks by its bare name.
    [Fact]
    public void ForUpdate_OfQualifiedUnaliasedTable_Executes()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string schema = connection.ExecuteScalar<string>("SELECT DATABASE()")!;
        DbTable t = new($"{schema}.users");
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> ids = connection.Query<int>(
            Select(t.Column("id")).From(t).Where(t.Column("id") == 1).ForUpdate(Of(t)),
            transaction);

        Assert.Equal(new[] { 1 }, ids);
        transaction.Rollback();
    }

    // #590: MySQL 8.0 runs a locked SELECT as a subquery, a CTE body and a derived table.
    [Fact]
    public void LockedSubqueryInIn_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders FOR UPDATE)").ToList();
    }

    [Fact]
    public void LockedCteBody_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "WITH c AS (SELECT id FROM users FOR UPDATE) SELECT id FROM c").ToList();
    }

    [Fact]
    public void LockedDerivedTable_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users FOR UPDATE) d").ToList();
    }

    // #662: whether the engine runs a window on a DISTINCT aggregate decides
    // whether the library may reject the pairing as valid on no dialect (ADR 0007).
    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.Every),
        MemberType = typeof(DistinctAggregateWindows))]
    public void DistinctAggregateWindow_IsRejectedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Execute(sql));
    }

    // #666: ANY is half of a comparison, valid nowhere on its own (MySQL 8.0).
    [Fact]
    public void QuantifiedOperand_OutsideAComparison_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT 1 = ANY (SELECT 1)"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar("SELECT ANY (SELECT 1)"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT 'a' LIKE ANY (SELECT 'a') ESCAPE '!'"));
    }

    // #675: the row alias holds the target's proposed row alone (MySQL 8.0), so
    // Excluded(...) over another table's column would read the target's same-named one;
    // another table's value is read through a subquery.
    [Fact]
    public void PseudoRowColumn_ReadsTheTargetRowOnly()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(
            "CREATE TEMPORARY TABLE pr_t (a INT PRIMARY KEY, b INT); "
                + "INSERT INTO pr_t VALUES (1, 10); "
                + "CREATE TEMPORARY TABLE pr_u (a INT, b INT); INSERT INTO pr_u VALUES (1, 99);");

        try
        {
            connection.Execute(
                "INSERT INTO pr_t (a, b) VALUES (1, 5) AS new ON DUPLICATE KEY UPDATE b = new.b");
            Assert.Equal(5, connection.ExecuteScalar<int>("SELECT b FROM pr_t"));
            connection.Execute(
                "INSERT INTO pr_t (a, b) VALUES (1, 5) AS new ON DUPLICATE KEY UPDATE b = "
                    + "(SELECT u.b FROM pr_u u WHERE u.a = new.a)");
            Assert.Equal(99, connection.ExecuteScalar<int>("SELECT b FROM pr_t"));
        }
        finally
        {
            connection.Execute("DROP TEMPORARY TABLE pr_t; DROP TEMPORARY TABLE pr_u");
        }
    }

    // docs/analyzer.md (SQLA0205): MySQL compares a text column to a number as
    // numbers, so every text converting to it matches; a SELECT does not fail on a text
    // that is no number, and a DELETE does under the default strict sql_mode.
    [Fact]
    public void TextColumnComparedToNumber_MatchesEveryTextConvertingToIt()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute("DROP TEMPORARY TABLE IF EXISTS zips");
        connection.Execute("CREATE TEMPORARY TABLE zips (z VARCHAR(30))");
        connection.Execute(
            "INSERT INTO zips VALUES ('01500001'), ('1500001 Nowhere St'), ('abc')");

        IEnumerable<string> matched = connection.Query<string>(
            "SELECT z FROM zips WHERE z = @n ORDER BY z", new { n = 1500001 });

        Assert.Equal(new[] { "01500001", "1500001 Nowhere St" }, matched);
        connection.Execute("DROP TEMPORARY TABLE IF EXISTS words");
        connection.Execute("CREATE TEMPORARY TABLE words (z VARCHAR(30))");
        connection.Execute("INSERT INTO words VALUES ('01500001'), ('abc')");
        Assert.ThrowsAny<DbException>(() => connection.Execute(
            "DELETE FROM words WHERE z = @n", new { n = 1500001 }));
    }

    // docs/analyzer.md (SQLA0205): MySQL converts text compared to a temporal column to
    // a date and time, so a well-formed spelling matches as one and a malformed one fails.
    [Fact]
    public void TemporalColumnComparedToText_ConvertsTheText()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        long Matching(string text) => connection.ExecuteScalar<long>(
            Select(Count(u.Id)).From(u).Where(u.CreatedAt == text));
        long canonical = Matching("2020-03-15 10:30:00");

        Assert.True(canonical > 0);
        Assert.Equal(canonical, Matching("2020-03-15 10:30"));
        Assert.Equal(canonical, Matching("2020-03-15 10:30:00.000"));
        Assert.ThrowsAny<DbException>(() => Matching("abc"));
    }

    // docs/analyzer.md (SQLA0205): MySQL compares a binary column to text as bytes.
    [Fact]
    public void BinaryColumnComparedToText_ComparesTheBytes()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute("DROP TEMPORARY TABLE IF EXISTS bins");
        connection.Execute("CREATE TEMPORARY TABLE bins (id INT, b VARBINARY(10))");
        connection.Execute("INSERT INTO bins VALUES (1, 'abc'), (2, 'ABC')");

        Assert.Equal(
            new[] { 1 },
            connection.Query<int>("SELECT id FROM bins WHERE b = @s", new { s = "abc" }));
    }

    // docs/analyzer.md (SQLA0200): a zero date, which only a non-strict sql_mode stores,
    // is what IS NULL on a NOT NULL DATE or DATETIME column finds at a WHERE's top level
    // or under its AND, whatever the reading session's mode; under NOT it is false.
    [Fact]
    public void IsNullOnNotNullDate_FindsTheZeroDate()
    {
        using IDbConnection lenient = _fixture.OpenConnection();
        lenient.Execute("DROP TABLE IF EXISTS zero_dates");
        lenient.Execute(
            "CREATE TABLE zero_dates (id INT, d DATE NOT NULL, dt DATETIME NOT NULL)");
        try
        {
            lenient.Execute("SET SESSION sql_mode = ''");
            lenient.Execute(
                "INSERT INTO zero_dates VALUES (1, '0000-00-00', '0000-00-00 00:00:00'), "
                + "(2, '2024-01-01', '2024-01-01 00:00:00')");
            using IDbConnection strict = _fixture.OpenConnection();
            IEnumerable<int> Ids(string where) => strict.Query<int>(
                $"SELECT id FROM zero_dates WHERE {where} ORDER BY id");

            Assert.ThrowsAny<DbException>(() => strict.Execute(
                "INSERT INTO zero_dates VALUES (3, '0000-00-00', '2024-01-01 00:00:00')"));
            Assert.Equal(new[] { 1 }, Ids("d IS NULL"));
            Assert.Equal(new[] { 1 }, Ids("dt IS NULL"));
            Assert.Equal(new[] { 1 }, Ids("d IS NULL AND id > 0"));
            Assert.Equal(new[] { 1, 2 }, Ids("NOT (d IS NULL)"));
        }
        finally
        {
            lenient.Execute("DROP TABLE IF EXISTS zero_dates");
        }
    }

    // docs/query-statements.md: MySQL rejects a DML subquery reading the target's own
    // table (ER_UPDATE_TABLE_USED); the joined forms read it instead.
    [Fact]
    public void UpdateAndDelete_SubqueryReadingTheTarget_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        UsersTable t = new("t");
        UsersTable i = new("i");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() => connection.Execute(
            DeleteFrom(u).Where(u.Id.In(Select(i.Id).From(i).Where(i.Name == "none"))),
            transaction));
        connection.Execute(
            DeleteFrom(t).From(t).InnerJoin(i).On(t.Id == i.Id).Where(i.Name == "none"),
            transaction);
        Assert.ThrowsAny<DbException>(() => connection.Execute(
            Update(u).Set(u.Age == 0).Where(u.Id.In(Select(i.Id).From(i).Where(i.Name == "none"))),
            transaction));
        connection.Execute(
            Update(t).InnerJoin(i).On(t.Id == i.Id).Set(t.Age == 0).Where(i.Name == "none"),
            transaction);
        transaction.Rollback();
    }

    // docs/query-statements.md: MySQL has no FULL JOIN, so after an unaliased table
    // it reads FULL as that table's alias and runs an inner join.
    [Fact]
    public void FullJoin_UnaliasedLeftTable_ReadsFullAsItsAlias()
    {
        UsersTable u = new();
        OrdersTable o = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long viaFull = connection.ExecuteScalar<long>(
            "SELECT COUNT(FULL.id) FROM orders FULL JOIN users ON FULL.user_id = users.id");
        long inner = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM orders INNER JOIN users ON orders.user_id = users.id");

        Assert.Equal(inner, viaFull);
        Assert.ThrowsAny<DbException>(() => connection.Query<string>(
            Select(u.Name).From(o).FullJoin(u).On(o.UserId == u.Id)));
    }

    // docs/functions.md: the RegexpReplace warning; occurrence 0 replaces every match.
    [Fact]
    public void RegexpReplace_ThreeArguments_ReplacesEveryMatch()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0"))));
        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0", 1))));
        Assert.Equal("x00x0", connection.ExecuteScalar<string>(
            Select(RegexpReplace("xooxo", "o", "0", 1, 0))));
    }

    // docs/functions.md: the Length warning.
    [Fact]
    public void Length_NonAsciiCharacter_CountsBytes()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(2, Convert.ToInt32(connection.ExecuteScalar(Select(Length("\u00e9")))));
        Assert.Equal(1, Convert.ToInt32(connection.ExecuteScalar(Select(CharLength("\u00e9")))));
    }

    // docs/functions.md: GREATEST and LEAST return NULL for a NULL argument here.
    [Fact]
    public void GreatestAndLeast_NullArgument_ReturnsNull()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Null(connection.ExecuteScalar<int?>(Select(Greatest(Null, 3, 1))));
        Assert.Null(connection.ExecuteScalar<int?>(Select(Least(Null, 3, 1))));
    }

    // docs/query-statements.md: INTERSECT binds tighter than UNION and EXCEPT here.
    [Fact]
    public void SetOperators_IntersectAfterUnionOrExcept_BindsFirst()
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
        IEnumerable<int> exceptIds = connection.Query<int>(
            Select(a.Id).From(a).Where(a.Id == 1)
            .Except
            .Select(b.Id).From(b).Where(b.Id == 1)
            .Intersect
            .Select(c.Id).From(c).Where(c.Id == 2));

        Assert.Equal(new[] { 1 }, ids);
        Assert.Equal(new[] { 1 }, exceptIds);
    }

    // docs/expressions.md: a bound backslash escapes the next pattern character here
    // unless ESCAPE names another escape character.
    [Fact]
    public void Like_BoundBackslash_EscapesTheNextCharacter()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long plain = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(Bind(@"a\b").Like(@"a\b"))));
        long escaped = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(Bind(@"a\b").Like(@"a\b").Escape('!'))));

        Assert.Equal(0, plain);
        Assert.Equal(5, escaped);
    }

    // docs/expressions.md: MySQL's -> / ->> take a JSON path; a bare key or an
    // integer index is rejected.
    [Fact]
    public void JsonArrow_KeyOrIndexWithoutAPath_IsRejectedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal("Alice", connection.ExecuteScalar<string>(
            Select(JsonArrowText(u.Data, "$.name")).From(u).Where(u.Id == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(JsonArrowText(u.Data, "name")).From(u).Where(u.Id == 1)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            Select(JsonArrow(u.Data, 0)).From(u).Where(u.Id == 1)));
    }

    // docs/functions.md: under NO_BACKSLASH_ESCAPES the backslash SqlArtisan doubles
    // in a forced literal stays doubled, and an ESCAPE character fails.
    [Fact]
    public void GroupConcat_BackslashSeparator_NoBackslashEscapes_KeepsBothBackslashes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        ISqlBuilder Query() =>
            Select(GroupConcat(u.Name, Sql.OrderBy(u.Id), Separator(@"\")))
            .From(u)
            .Where(u.Id <= 2);

        Assert.Equal(@"Alice\Bob", connection.ExecuteScalar<string>(Query()));
        connection.Execute("SET SESSION sql_mode = CONCAT(@@sql_mode, ',NO_BACKSLASH_ESCAPES')");
        try
        {
            Assert.Equal(@"Alice\\Bob", connection.ExecuteScalar<string>(Query()));
            Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
                Select(Count(u.Id)).From(u).Where(Bind("a").Like("a").Escape('\\'))));
        }
        finally
        {
            connection.Execute("SET SESSION sql_mode = @@GLOBAL.sql_mode");
        }
    }
}
