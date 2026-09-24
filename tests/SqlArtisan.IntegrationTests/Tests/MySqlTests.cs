using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan;
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

        // (data ->> '$.name') — MySQL accepts a bound parameter as the path, so
        // the key binds normally; ->> returns the unquoted scalar.
        string name = connection
            .Query<string>(Select(JsonArrowText(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
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

    [Fact] // #264: SQLA0102's live proof. GROUPING() under WITH ROLLUP is proven by
           // the dialect sweep's MySQL branch; the missing suffix is the only difference.
    public void ContextRule_GroupingWithoutWithRollup_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(Grouping(u.DepartmentId)).From(u).GroupBy(u.DepartmentId)));
    }

    [Fact] // #436: SQLA0102's live proof for the Interval context rule — the dialect
           // sweep proves the positioned form; only the arithmetic wrapper differs.
    public void ContextRule_IntervalOutsideArithmetic_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() => connection.Query<int>(
            Select(Interval(30, DateTimePart.Day)).From(u)));
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

    // The live twin of SelectBuilder's bare-OFFSET guard (ADR 0011).
    [Fact]
    public void Pagination_OffsetWithoutLimit_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY id OFFSET 1"));
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

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void GroupByOutOfRangeOrdinal_IsRejectedByTheEngine(string ordinal)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            $"SELECT department_id FROM users GROUP BY {ordinal}"));
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

    // #520: why only row-limit-then-lock is offered. The reverse order is a parse
    // error here, and no builder chain can reach it, so the twin is raw SQL; the
    // accepted order beside it is LimitedForUpdate_Executes.
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
}
