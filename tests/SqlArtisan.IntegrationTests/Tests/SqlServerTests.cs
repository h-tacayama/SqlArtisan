using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan;
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

    // #613's string-left + emits `@0 + name`, which T-SQL reads as concatenation.
    [Fact]
    public void StringLeftAddition_Concatenates()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        string name = connection.ExecuteScalar<string>("SELECT name FROM users WHERE id = 1")!;

        string? concatenated = connection.ExecuteScalar<string>(
            Select("Dr. " + u.Name).From(u).Where(u.Id == 1));

        Assert.Equal("Dr. " + name, concatenated);
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

    [Fact(Skip = "SQL Server requires an ORDER BY in a value analytic function's "
        + "window; ContextRule_ValueWindowWithoutOrderBy_Rejected asserts the rejection here.")]
    public override void WindowFunction_ValueFamilyPartitionOnly_Executes()
    {
    }

    [Fact(Skip = "SQL Server requires an ORDER BY in a value analytic function's "
        + "window; ContextRule_ValueWindowWithoutOrderBy_Rejected asserts the rejection here.")]
    public override void WindowFunction_ValueFamilyWholeResultSet_Executes()
    {
    }

    [Fact] // SQLA0102 (#521, #539): the live twin for CheckUnorderedWindowRequiresOrderBy.
    public void ContextRule_ValueWindowWithoutOrderBy_Rejected()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // The same window with an ORDER BY runs, so only the missing ORDER BY
        // is what SQL Server 2022 rejects.
        connection.ExecuteScalar(
            Select(FirstValue(u.Age).Over(PartitionBy(u.DepartmentId).OrderBy(u.Id))).From(u));

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            Select(FirstValue(u.Age).Over(PartitionBy(u.DepartmentId))).From(u)));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            Select(LastValue(u.Age).Over(PartitionBy(u.DepartmentId))).From(u)));

        connection.ExecuteScalar(Select(FirstValue(u.Age).Over(OrderBy(u.Id))).From(u));

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            Select(FirstValue(u.Age).Over()).From(u)));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            Select(LastValue(u.Age).Over()).From(u)));
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

    // SQLA0102's twin (#582): T-SQL bounds the branches per clause-and-action pair,
    // a condition on each notwithstanding.
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

        connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN UPDATE SET name = t.name "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < -1 THEN DELETE;",
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

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN MATCHED AND t.id < 0 THEN DELETE "
                + "WHEN MATCHED AND t.id < -1 THEN DELETE;",
            transaction: transaction));

        Assert.ThrowsAny<Exception>(() => connection.Execute(
            "MERGE INTO users AS t USING (SELECT 1 AS id, 'x' AS name) AS s ON t.id = s.id "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < 0 THEN UPDATE SET name = t.name "
                + "WHEN NOT MATCHED BY SOURCE AND t.id < -1 THEN UPDATE SET name = t.name;",
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

    [Fact(Skip = "SQL Server rejects a WITH inside INSERT ... SELECT; "
        + "ContextRule_InsertSelectWith_Rejected asserts the rejection here.")]
    public override void Cte_WithInsideInsertSelect_Executes()
    {
    }

    // SQLA0102 (#569): Microsoft's INSERT reference says the SELECT cannot hold a
    // CTE. The leading WITH runs, so only the WITH's position is rejected; its key
    // differs from the probe's, so a duplicate key cannot pass for the rejection.
    [Fact]
    public void ContextRule_InsertSelectWith_Rejected()
    {
        UsersTable u = new();
        Cte c = new("c");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            With(c.As(
                    Select((u.Id + 800).As(c.Column("id")), u.Name.As(c.Column("name")))
                    .From(u)
                    .Where(u.Id == 1)))
                .InsertInto(u, u.Id, u.Name)
                .Select(c.Column("id"), c.Column("name"))
                .From(c),
            transaction);

        Assert.ThrowsAny<DbException>(
            () => connection.Execute(MidChainWithInsert(), transaction));
        transaction.Rollback();
    }

    // SQLA0102 (#587): SQL Server filters a MERGE branch on its WHEN and takes no
    // WHERE after the action. The unfiltered actions run, so only the WHERE is rejected.
    [Fact]
    public void ContextRule_MergeActionWhere_Rejected()
    {
        UsersTable t = new("t");
        UsersTable s = new("s");
        using IDbConnection connection = _fixture.OpenConnection();

        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            connection.Execute(
                MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenMatched().ThenUpdateSet(t.Name == s.Name),
                transaction);
            connection.Execute(
                MergeInto(t).Using(s).On(t.Id == s.Id + 900)
                    .WhenNotMatched().ThenInsert(t.Id, t.Name).Values(s.Id + 900, s.Name),
                transaction);
            transaction.Rollback();
        }

        // One transaction each, as on the PostgreSQL lane.
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            Assert.ThrowsAny<DbException>(() => connection.Execute(
                MergeInto(t).Using(s).On(t.Id == s.Id)
                    .WhenMatched().ThenUpdateSet(t.Name == s.Name).Where(t.Age < 35),
                transaction));
            transaction.Rollback();
        }

        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            Assert.ThrowsAny<DbException>(() => connection.Execute(
                MergeInto(t).Using(s).On(t.Id == s.Id + 900)
                    .WhenNotMatched().ThenInsert(t.Id, t.Name).Values(s.Id + 900, s.Name)
                    .Where(s.Age < 35),
                transaction));
            transaction.Rollback();
        }
    }

    // The walk rejects a held stage's OUTPUT after OUTPUT ... INTO on every dialect
    // (ADR 0011); SQL Server runs the text, one of each.
    [Fact]
    public void OutputAfterOutputInto_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<int> ids = connection.Query<int>(
            "DELETE FROM users OUTPUT DELETED.id INTO output_archive (id) OUTPUT DELETED.id "
                + "WHERE id = 3",
            transaction: transaction);

        Assert.Equal(new[] { 3 }, ids);
        transaction.Rollback();
    }

    // The Build() guard's reason (#569): SQL Server has TOP and OFFSET/FETCH, but
    // not in one query.
    [Fact]
    public void TopWithOffsetFetch_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT TOP (1) id FROM users ORDER BY id");
        connection.ExecuteScalar(
            "SELECT id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY");

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT TOP (1) id FROM users ORDER BY id OFFSET 0 ROWS"));
        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT TOP (1) id FROM users ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
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

    // The raw half of the docs' claim: T-SQL's MERGE grammar leads with an
    // optional common_table_expression list.
    [Fact]
    public void LeadingWithBeforeMerge_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "WITH c AS (SELECT 1 AS id, 'x' AS name) "
                + "MERGE INTO users AS t USING c AS s ON t.id = s.id "
                + "WHEN MATCHED THEN UPDATE SET name = s.name;",
            transaction: transaction);
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

    // The fact public-api-design.md's decline of the columnless OUTPUT rests on:
    // T-SQL takes the form, so declining it is ergonomic, not a missing spelling.
    [Fact]
    public void OutputWithoutColumnList_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the column-list shape the builder already reaches.
        Assert.Equal(
            901,
            connection.ExecuteScalar<int>(
                "INSERT INTO users (id, name) OUTPUT INSERTED.id VALUES (901, 'x')",
                transaction: transaction));

        Assert.Equal(
            902,
            connection.ExecuteScalar<int>(
                "INSERT INTO users OUTPUT INSERTED.id "
                    + "VALUES (902, 'y', 30, 10, NULL, 1, NULL)",
                transaction: transaction));
        transaction.Rollback();
    }

    // Where OUTPUT goes on an INSERT, pinned from both sides with the twin below —
    // the position #542's guard message will have to state.
    [Fact]
    public void OutputBeforeColumnList_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.ExecuteScalar<int>(
                "INSERT INTO users OUTPUT INSERTED.id (id, name) VALUES (903, 'z')",
                transaction: transaction));
        transaction.Rollback();
    }

    // The trailing side: #542's held builder appends OUTPUT after VALUES.
    [Fact]
    public void OutputAfterValues_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        // Acceptance control: the same statement with OUTPUT between the two halves.
        Assert.Equal(
            906,
            connection.ExecuteScalar<int>(
                "INSERT INTO users (id, name) OUTPUT INSERTED.id VALUES (906, 'q')",
                transaction: transaction));

        Assert.ThrowsAny<DbException>(() =>
            connection.ExecuteScalar<int>(
                "INSERT INTO users (id, name) VALUES (907, 'r') OUTPUT INSERTED.id",
                transaction: transaction));
        transaction.Rollback();
    }

    // What a positional row skips, one column kind per twin: an identity column
    // and a computed one take no value, so naming the rest writes the same row.
    [Fact]
    public void PositionalValues_SkipAnIdentityColumn()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "CREATE TABLE #identity (id INT IDENTITY(1, 1), a INT, b NVARCHAR(10))",
            transaction: transaction);

        (int, string) positional = connection.QuerySingle<(int, string)>(
            "INSERT INTO #identity OUTPUT INSERTED.a, INSERTED.b VALUES (5, 'x')",
            transaction: transaction);
        (int, string) named = connection.QuerySingle<(int, string)>(
            "INSERT INTO #identity (a, b) OUTPUT INSERTED.a, INSERTED.b VALUES (5, 'x')",
            transaction: transaction);

        Assert.Equal((5, "x"), positional);
        Assert.Equal(positional, named);
        transaction.Rollback();
    }

    [Fact]
    public void PositionalValues_SkipAComputedColumn()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "CREATE TABLE #computed (a INT, b NVARCHAR(10), c AS (a * 2))",
            transaction: transaction);

        (int, string, int) positional = connection.QuerySingle<(int, string, int)>(
            "INSERT INTO #computed OUTPUT INSERTED.a, INSERTED.b, INSERTED.c VALUES (5, 'x')",
            transaction: transaction);
        (int, string, int) named = connection.QuerySingle<(int, string, int)>(
            "INSERT INTO #computed (a, b) OUTPUT INSERTED.a, INSERTED.b, INSERTED.c "
                + "VALUES (5, 'x')",
            transaction: transaction);

        Assert.Equal((5, "x", 10), positional);
        Assert.Equal(positional, named);
        transaction.Rollback();
    }

    // A rowversion column is the exception: a positional row must still fill its
    // slot, while a column list simply leaves it out.
    [Fact]
    public void PositionalValues_DoNotSkipARowversionColumn()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "CREATE TABLE #rowversion (a INT, b NVARCHAR(10), v ROWVERSION)",
            transaction: transaction);

        // Acceptance control: the column list that leaves the rowversion out.
        Assert.Equal(
            5,
            connection.ExecuteScalar<int>(
                "INSERT INTO #rowversion (a, b) OUTPUT INSERTED.a VALUES (5, 'x')",
                transaction: transaction));

        Assert.ThrowsAny<DbException>(() =>
            connection.ExecuteScalar<int>(
                "INSERT INTO #rowversion OUTPUT INSERTED.a VALUES (6, 'y')",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void PositionalValues_FillARowversionSlotWithDefault()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "CREATE TABLE #rowversion (a INT, b NVARCHAR(10), v ROWVERSION)",
            transaction: transaction);

        Assert.Equal(
            7,
            connection.ExecuteScalar<int>(
                "INSERT INTO #rowversion OUTPUT INSERTED.a VALUES (7, 'z', DEFAULT)",
                transaction: transaction));
        transaction.Rollback();
    }

    // ADR 0016 (#664): why StringAgg inlines its separator — a bound one travels as
    // nvarchar, rejected over a varchar input, while the literal runs over both.
    [Fact]
    public void StringAgg_BoundSeparatorOverVarchar_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        connection.Execute(
            "CREATE TABLE #agg_664 (v VARCHAR(10), n NVARCHAR(10)); "
                + "INSERT INTO #agg_664 VALUES ('a', N'a'), ('b', N'b');",
            null,
            transaction);

        Assert.Equal("a,b", connection.QuerySingle<string>(
            "SELECT STRING_AGG(v, ',') WITHIN GROUP (ORDER BY v) FROM #agg_664",
            null,
            transaction));
        Assert.Equal("a,b", connection.QuerySingle<string>(
            "SELECT STRING_AGG(n, @sep) WITHIN GROUP (ORDER BY n) FROM #agg_664",
            new { sep = "," },
            transaction));
        Assert.ThrowsAny<DbException>(() => connection.QuerySingle<string>(
            "SELECT STRING_AGG(v, @sep) FROM #agg_664", new { sep = "," }, transaction));
        transaction.Rollback();
    }

    // #664: a small fraction renders in its invariant round-trip spelling, an exponent,
    // where the grammar names numeric_literal.
    [Fact]
    public void PercentileCont_ExponentFraction_IsAcceptedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        SqlStatement sql =
            Select(PercentileCont(0.00001).WithinGroup(OrderBy(u.Age)).Over())
            .From(u)
            .Build(Dbms.SqlServer);

        Assert.Contains("PERCENTILE_CONT(1E-05)", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
        Assert.NotNull(connection.Query<double?>(sql.Text).First());
    }

    // #661: Trim(s, c) emits TRIM(c FROM s), which runs below compatibility level 160,
    // where the BOTH keyword the node used to add is refused; 140 is SQL Server 2017's.
    [Theory]
    [InlineData(150)]
    [InlineData(140)]
    public void Trim_TrimCharacters_RunBelowCompatibilityLevel160(int level)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string database = $"probe_cl{level}";
        connection.Execute(
            $"IF DB_ID('{database}') IS NULL CREATE DATABASE {database}; "
                + $"ALTER DATABASE {database} SET COMPATIBILITY_LEVEL = {level};");
        connection.ChangeDatabase(database);

        Assert.Equal("a", connection.QuerySingle<string>(Select(Trim("xxaxx", "x"))));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT TRIM(BOTH 'x' FROM 'xxaxx')"));
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

    // ADR 0016 (#663): SQL Server 2022 turns a character the code page lacks into '?'
    // in a plain literal (SQL_Latin1_General_CP1_CI_AS here): a JSON path or ESCAPE
    // needs N'...', and a STRING_AGG separator, kept plain, loses it.
    [Fact]
    public void NonAsciiForcedLiteral_PlainForm_LosesTheCharacter()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        string doc = "{\"名前\":\"x\",\"住所\":{\"a\":1}}";

        Assert.Null(connection.ExecuteScalar<string>(
            "SELECT JSON_VALUE(@doc, '$.\"名前\"')", new { doc }));
        Assert.Null(connection.ExecuteScalar<string>(
            "SELECT JSON_QUERY(@doc, '$.\"住所\"')", new { doc }));
        Assert.Equal(0, connection.ExecuteScalar<int>(
            "SELECT CASE WHEN N'50%' LIKE N'50＼%' ESCAPE '＼' THEN 1 ELSE 0 END"));

        Assert.Equal("x", connection.ExecuteScalar<string>(
            Select(JsonValue(Bind(doc), "$.\"名前\""))));
        Assert.Equal("{\"a\":1}", connection.ExecuteScalar<string>(
            Select(JsonQuery(Bind(doc), "$.\"住所\""))));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            Select(Case(When(Bind("50%").Like("50＼%").Escape('＼')).Then(1), Else(0)))));

        using IDbTransaction transaction = connection.BeginTransaction();
        connection.Execute(
            "CREATE TABLE #agg_663 (n NVARCHAR(10)); INSERT INTO #agg_663 VALUES (N'a'), (N'b');",
            null,
            transaction);
        DbTable agg = new("#agg_663", "g");
        Assert.Equal("a?b", connection.ExecuteScalar<string>(
            Select(StringAgg(agg.Column("n"), "、").WithinGroup(OrderBy(agg.Column("n"))))
            .From(agg),
            transaction));
        transaction.Rollback();
    }

    // ADR 0016 (#663): where the code page holds the character (Japanese_CI_AS), the N
    // form returns the rows the plain form returned and keeps the index on the plain
    // path; STRING_AGG stays plain, as N is rejected over varchar even there.
    [Fact]
    public void NonAsciiForcedLiteral_OnACodePageHoldingIt_KeepsWhatThePlainFormRan()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(
            "IF DB_ID('probe_ja_663') IS NULL "
                + "CREATE DATABASE probe_ja_663 COLLATE Japanese_CI_AS;");
        connection.ChangeDatabase("probe_ja_663");
        connection.Execute(
            "IF OBJECT_ID('dbo.j663') IS NOT NULL DROP TABLE dbo.j663; "
                + "CREATE TABLE dbo.j663 (id INT IDENTITY PRIMARY KEY, doc NVARCHAR(400), "
                + "v AS JSON_VALUE(doc, '$.\"名前\"')); "
                + "INSERT INTO dbo.j663 (doc) SELECT TOP (5000) "
                + "CONCAT(N'{\"名前\":\"k', x.n, N'\"}') "
                + "FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n "
                + "FROM sys.all_objects a CROSS JOIN sys.all_objects b) x; "
                + "CREATE INDEX ix_j663_v ON dbo.j663 (v); "
                + "IF OBJECT_ID('dbo.s663') IS NOT NULL DROP TABLE dbo.s663; "
                + "CREATE TABLE dbo.s663 (s VARCHAR(10), k VARCHAR(10)); "
                + "INSERT INTO dbo.s663 VALUES ('50%', 'a'), ('509', 'b');");
        DbTable j = new("j663", "j");
        DbTable v = new("s663", "s");

        string national = Select(j.Column("id")).From(j)
            .Where(JsonValue(j.Column("doc"), "$.\"名前\"") == "k42")
            .Build(Dbms.SqlServer).Text
            .Replace("@0", "N'k42'", StringComparison.Ordinal);
        string plain = national.Replace("N'$.", "'$.", StringComparison.Ordinal);
        Assert.Contains("N'$.\"名前\"'", national);
        Assert.Equal(
            connection.Query<int>(plain).Single(), connection.Query<int>(national).Single());
        connection.Execute("SET SHOWPLAN_XML ON");
        string plan = connection.QuerySingle<string>(national);
        connection.Execute("SET SHOWPLAN_XML OFF");
        Assert.Contains("ix_j663_v", plan);

        int plainCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.s663 WHERE s LIKE N'50＼%' ESCAPE '＼'");
        Assert.Equal(1, plainCount);
        Assert.Equal(
            plainCount,
            connection.ExecuteScalar<int>(
                Select(Count(v.Column("s"))).From(v)
                .Where(v.Column("s").Like("50＼%").Escape('＼'))));
        Assert.Equal("a、b", connection.ExecuteScalar<string>(
            Select(StringAgg(v.Column("k"), "、").WithinGroup(OrderBy(v.Column("k")))).From(v)));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar<string>(
            "SELECT STRING_AGG(k, N'、') FROM dbo.s663"));
    }

    // #666: ANY is half of a comparison, valid nowhere on its own (SQL Server 2022).
    [Fact]
    public void QuantifiedOperand_OutsideAComparison_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT CASE WHEN 1 = ANY (SELECT 1) THEN 1 END"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar("SELECT ANY (SELECT 1)"));
        Assert.ThrowsAny<DbException>(() => connection.ExecuteScalar(
            "SELECT CASE WHEN 'a' LIKE ANY (SELECT 'a') ESCAPE '!' THEN 1 END"));
    }

    // #675: INSERTED holds the target's post-image alone (SQL Server 2022), so
    // Inserted(...) over a joined table's column would read the target's same-named one;
    // the joined column is output directly.
    [Fact]
    public void PseudoRowColumn_ReadsTheTargetRowOnly()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();
        connection.Execute(
            "CREATE TABLE #pr_t (a INT, b INT); CREATE TABLE #pr_u (a INT, b INT); "
                + "INSERT INTO #pr_t VALUES (1, 10); INSERT INTO #pr_u VALUES (1, 99);",
            null,
            transaction);

        (int inserted, int joined) = connection.QuerySingle<(int, int)>(
            "UPDATE t SET b = u.b + 1 OUTPUT INSERTED.b, u.b FROM #pr_t t "
                + "JOIN #pr_u u ON t.a = u.a",
            null,
            transaction);
        Assert.Equal((100, 99), (inserted, joined));
        transaction.Rollback();
    }
}
