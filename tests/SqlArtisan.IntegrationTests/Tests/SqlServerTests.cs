using System.Data;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "SqlServer")]
public sealed class SqlServerTests : IntegrationTestBase, IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerTests(SqlServerFixture fixture) : base(fixture)
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

    [Fact]
    public void Sequence_NextValueFor_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        long next = Convert.ToInt64(connection.ExecuteScalar(
            Select(NextValueFor("test_seq")).From(u).Where(u.Id == 1)));

        Assert.True(next >= 1);
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

    [Fact]
    public void Merge_WhenNotMatchedBySource_DeletesUnmatched()
    {
        UsersTable t = new("t");
        OrdersTable o = new("o");
        UsersTable c = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // NOT MATCHED BY SOURCE deletes the users no order references — only Dave.
        connection.Execute(
            MergeInto(t)
                .Using(o)
                .On(t.Id == o.UserId)
                .WhenNotMatchedBySource().ThenDelete(),
            transaction);

        long count = Convert.ToInt64(
            connection.ExecuteScalar(Select(Count(c.Id)).From(c), transaction));

        Assert.Equal(4, count);
        transaction.Rollback();
    }

    [Fact] // Regression for #168: STRING_AGG's separator is now emitted as an
           // inline literal, which SQL Server requires (it rejects a parameter).
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
    public void JsonValue_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // JSON_VALUE(data, '$.name') extracts a scalar from the NVARCHAR(MAX) column.
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

    [Fact] // #254: T-SQL takes a DML target alias only from a FROM clause, so
           // `UPDATE users AS "cu"` is a syntax error and Build(SqlServer) throws.
    public void AliasedDmlTarget_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The unaliased forms are valid T-SQL (so the table and columns are right),
        // run inside a rolled-back transaction to leave the seed untouched.
        using (IDbTransaction tx = connection.BeginTransaction())
        {
            connection.Execute("INSERT INTO users (id, name) VALUES (999, 'x')", transaction: tx);
            connection.Execute("UPDATE users SET name = name WHERE id = 1", transaction: tx);
            connection.Execute("DELETE FROM users WHERE id = -1", transaction: tx);
            tx.Rollback();
        }

        // Aliasing the target is the one difference SQL Server rejects; the rolled-back
        // transaction keeps a wrong grammar assumption out of the shared fixture.
        using (IDbTransaction probeTx = connection.BeginTransaction())
        {
            Assert.ThrowsAny<Exception>(() => connection.Execute(
                "INSERT INTO users AS \"cu\" (id, name) VALUES (999, 'x')", transaction: probeTx));
            Assert.ThrowsAny<Exception>(() => connection.Execute(
                "UPDATE users AS \"cu\" SET name = 'x' WHERE \"cu\".id = 1", transaction: probeTx));
            Assert.ThrowsAny<Exception>(() => connection.Execute(
                "DELETE FROM users AS \"cu\" WHERE \"cu\".id = 1", transaction: probeTx));
            probeTx.Rollback();
        }
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

    [Fact] // #400: anchors the SQLA0102 percentile rule — SQL Server exposes the
           // percentiles only as window functions, so a bare WITHIN GROUP fails.
    public void BarePercentileWithinGroup_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute(
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) OVER () FROM users");

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "SELECT PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY age) FROM users"));
    }

    [Fact] // #400: anchors the SQLA0102 INSERTED/DELETED rule — the OUTPUT clause
           // binds the pseudo-tables, so a reference outside one resolves to nothing.
    public void OutputPseudoTableOutsideOutput_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute("DELETE FROM users OUTPUT DELETED.id WHERE id = -1");

        // A wrapping function keeps the reference bound, so the rule must not warn there.
        connection.Execute("DELETE FROM users OUTPUT COALESCE(DELETED.id, 0) WHERE id = -1");

        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT INSERTED.id FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.Execute("SELECT DELETED.id FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "DELETE FROM users WHERE DELETED.id = -1"));
    }

    [Fact] // #241 (GAP-19): SQL Server matches GROUP BY syntactically, so a
           // parameterized expression repeated with fresh markers fails with Msg 8120.
    public void GroupByBindMarkerMismatch_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The expression itself is valid (table and columns are right).
        connection.Execute(
            "SELECT CASE department_id WHEN @p0 THEN @p1 ELSE @p2 END FROM users",
            new { p0 = 10, p1 = "Low", p2 = "Other" });

        // The only difference — distinct markers in GROUP BY — is what SS rejects.
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
        // SQL Server requires the windowed OVER() form (matching MatrixSweepCatalog).
        connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY age) OVER () FROM users");

        // The only difference — an out-of-range fraction — is what SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT PERCENTILE_CONT(1.5) WITHIN GROUP (ORDER BY age) OVER () FROM users"));
    }

    [Fact] // ADR 0012 (#402): anchors WindowFrameGuard. NTH_VALUE is omitted —
           // SQL Server does not support it.
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 "
                + "PRECEDING) FROM users");

        // The only difference each time — the value-domain violation — is what SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT NTILE(0) OVER (ORDER BY age) FROM users"));
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

    [Fact] // SQLA0104 (#449): anchors SqlServerDatepartFields in DatepartValidity.cs —
           // EPOCH is a PostgreSQL spelling, not a T-SQL datepart.
    public void Datepart_EpochDatepart_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed datepart is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT DATEPART(year, created_at) FROM users");

        // The only difference — the datepart DATEPART doesn't have — is what
        // SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT DATEPART(epoch, created_at) FROM users"));
    }

    [Fact] // SQLA0104 (#449): anchors SqlServerDateTruncFields in DatepartValidity.cs —
           // per learn.microsoft.com DATETRUNC rejects weekday though DATEPART takes it.
    public void Datetrunc_WeekdayDatepart_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The listed datepart is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT DATETRUNC(day, created_at) FROM users");

        // The only difference — DATETRUNC's own documented exclusion — is what
        // SQL Server rejects.
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT DATETRUNC(weekday, created_at) FROM users"));
    }

    [Fact]
    public void JoinedUpdateFrom_Executes()
    {
        // The target alias comes from FROM, the T-SQL spelling that finally makes
        // an aliased UPDATE target valid on SQL Server.
        UsersTable u = new("u");
        OrdersTable o = new("o");
        UsersTable read = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).Set(u.Age == 999).From(u).InnerJoin(o).On(u.Id == o.UserId).Where(u.Id == 3),
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

    [Fact]
    public void OutputInto_ArchiveThenDelete_Executes()
    {
        // The single-statement archive-then-delete: OUTPUT ... INTO copies the
        // deleted rows into an archive table as they are removed.
        UsersTable u = new();
        OutputArchiveTable archive = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            DeleteFrom(u)
                .Output(Deleted(u.Id), Deleted(u.Name))
                .Into(archive, archive.Id, archive.Name)
                .Where(u.Id == 3),
            transaction);

        long usersLeft = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id == 3), transaction));
        string archivedName = connection
            .Query<string>(Select(archive.Name).From(archive).Where(archive.Id == 3), transaction)
            .Single();

        Assert.Equal(0, usersLeft);
        Assert.Equal("Carol", archivedName);
        transaction.Rollback();
    }

    [Fact]
    public void Output_Insert_ReturnsInsertedId()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        int inserted = connection
            .Query<int>(
                InsertInto(u, u.Id, u.Name).Output(Inserted(u.Id)).Values(700, "Grace"),
                transaction)
            .Single();

        Assert.Equal(700, inserted);
        transaction.Rollback();
    }

    // #523 item 7: DATEPART(weekday, ...) counts from whatever @@DATEFIRST the
    // session carries, so it has no fixed numbering basis to document.
    [Fact]
    public void DatepartWeekday_NumberingFollowsDateFirst()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // 2026-09-21 is a Monday: first day of the week under DATEFIRST 1,
        // second under DATEFIRST 7.
        Assert.Equal(
            1,
            connection.ExecuteScalar<int>(
                "SET DATEFIRST 1; SELECT DATEPART(weekday, '2026-09-21')"));
        Assert.Equal(
            2,
            connection.ExecuteScalar<int>(
                "SET DATEFIRST 7; SELECT DATEPART(weekday, '2026-09-21')"));
    }

    // ADR 0011 (#523): the SQL Server arms of the two constant-sort-key guards.
    // T-SQL rejects both forms the engine cannot read as a column position.
    [Fact]
    public void OrderByNonIntegerConstant_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // The integer ordinal is valid (so the table and columns are right).
        connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 2");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 2.5"));
    }

    [Fact]
    public void OrderByNegativeOrdinal_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id, name FROM users ORDER BY -1"));
    }

    // #523/#525: MERGE branch arity is a documented non-goal, not a guard —
    // T-SQL bounds the branches per clause-and-action pair.
    [Fact]
    public void MergeRepeatedBranchAction_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN MATCHED AND t.age > 0 THEN UPDATE SET name = t.name "
                + "WHEN MATCHED THEN DELETE;",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN MATCHED AND t.age > 0 THEN UPDATE SET name = t.name "
                + "WHEN MATCHED AND t.age > 1 THEN UPDATE SET name = t.name;",
            transaction: transaction));

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 999 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED AND s.id > 0 THEN INSERT (id, name) VALUES (s.id, s.name) "
                + "WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name);",
            transaction: transaction));

        transaction.Rollback();
    }

    // ADR 0012 non-goals (#523): both values travel to the engine — the row
    // count as a bind parameter, the LAG offset as text — so neither is guarded.
    [Fact]
    public void NegativeRowCount_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT TOP (0) id FROM users");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT TOP (-1) id FROM users"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT -1 ROWS ONLY"));
    }

    // #521 item 2: SQL Server is said to reject grouping by ordinal.
    [Fact]
    public void GroupByOrdinal_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT department_id FROM users GROUP BY department_id");

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT department_id FROM users GROUP BY 1"));
    }

    // Not just the in-range ordinal: every bare constant is refused here, which
    // is what a dialect-blind zero/negative guard rests on.
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2.5")]
    public void GroupByConstant_IsRejectedByTheEngine(string constant)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            $"SELECT department_id FROM users GROUP BY {constant}"));
    }

    // The SQL Server half of #532's per-dialect offset verdict (OFFSET ... ROWS
    // is its spelling); docs name every engine the behaviour holds on.
    [Fact]
    public void NegativeOffset_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id FROM users ORDER BY id OFFSET 0 ROWS");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT id FROM users ORDER BY id OFFSET -1 ROWS"));
    }

    [Fact]
    public void LagNegativeOffset_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT LAG(id, 1) OVER (ORDER BY id) FROM users");

        Assert.ThrowsAny<Exception>(() =>
            connection.ExecuteScalar("SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"));
    }

    // The second half of the same rule, and the one the guard withdrawn in #525
    // never modelled: a branch may not follow an unconditional one of its kind.
    [Fact]
    public void MergeRepeatedWhenBranch_NeedsAConditionOnTheEarlierBranch()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = t.name "
                + "WHEN MATCHED THEN DELETE;",
            transaction: transaction));

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET name = t.name "
                + "WHEN NOT MATCHED BY SOURCE THEN DELETE;",
            transaction: transaction));

        transaction.Rollback();
    }
}
