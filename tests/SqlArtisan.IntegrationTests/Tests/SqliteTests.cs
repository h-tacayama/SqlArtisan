using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

[Trait("Engine", "Sqlite")]
public sealed class SqliteTests : IntegrationTestBase, IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public SqliteTests(SqliteFixture fixture) : base(fixture)
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
    public void AggregateFilter_CountFilterWhere_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        // Ages are 30, 40, 50, 25, 35; three exceed 30. SQLite supports FILTER.
        int matching = connection
            .Query<int>(Select(Count(u.Id).Filter(u.Age > 30)).From(u))
            .Single();

        Assert.Equal(3, matching);
    }

    [Fact]
    public void JsonExtract_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string name = connection
            .Query<string>(Select(JsonExtract(u.Data, "$.name")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void JsonArrowText_ReadsScalar()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string city = connection
            .Query<string>(Select(JsonArrowText(u.Data, "$.city")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Equal("NYC", city);
    }

    [Fact]
    public void JsonArrow_ReadsNestedObject()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();

        string address = connection
            .Query<string>(Select(JsonArrow(u.Data, "$.address")).From(u).Where(u.Id == 1))
            .Single();

        Assert.Contains("10001", address);
    }

    [Fact]
    public void FullTextSearch_Fts5Match_Executes()
    {
        DbTable fts = new("users_fts");
        using IDbConnection connection = _fixture.OpenConnection();

        // The FTS5 virtual table is created (and dropped) here rather than in the
        // shared DDL: StandardDdl is shared with MySQL, which has no FTS5.
        connection.Execute("CREATE VIRTUAL TABLE users_fts USING fts5(name, bio)");

        try
        {
            connection.Execute(
                InsertInto(fts, fts.Column("name"), fts.Column("bio"))
                    .Values("Alice", "builds type-safe database queries"));
            connection.Execute(
                InsertInto(fts, fts.Column("name"), fts.Column("bio"))
                    .Values("Bob", "writes release notes"));

            // Bare-table target: users_fts MATCH :0
            string name = connection
                .Query<string>(
                    Select(fts.Column("name")).From(fts).Where(Match(fts, "database")))
                .Single();

            // Alias-qualified target: "f".users_fts MATCH :0 — a bare quoted
            // alias would fall back to a string literal and fail (#153 review).
            DbTable f = new("users_fts", "f");
            string aliased = connection
                .Query<string>(
                    Select(f.Column("name")).From(f).Where(Match(f, "database")))
                .Single();

            Assert.Equal("Alice", name);
            Assert.Equal("Alice", aliased);
        }
        finally
        {
            connection.Execute("DROP TABLE users_fts");
        }
    }

    [Fact] // SQLite's UPDATE ... FROM (3.33+); the bundled driver is well past it.
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
    public void WindowFrame_ValueDomainViolations_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        // Each in-range/well-ordered form is valid (so the table and column are right).
        connection.ExecuteScalar("SELECT NTILE(4) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar("SELECT NTH_VALUE(age, 1) OVER (ORDER BY age) FROM users");
        connection.ExecuteScalar(
            "SELECT SUM(age) OVER (ORDER BY age ROWS BETWEEN 3 PRECEDING AND 5 "
                + "PRECEDING) FROM users");

        // The only difference each time — the value-domain violation — is what SQLite rejects.
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

    // The live twin of SelectBuilder's bare-OFFSET guard (ADR 0011).
    [Fact]
    public void Pagination_OffsetWithoutLimit_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM users ORDER BY id OFFSET 1"));
    }

    // The permissive half of InsertBuilder's conflict-target guard (ADR 0011):
    // SQLite takes DO UPDATE without a target, so Build(Sqlite) must not throw.
    [Fact]
    public void Upsert_OnConflictDoUpdateWithoutTarget_IsAcceptedByTheEngine()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name)
                .Values(1, "AliceUpdated")
                .OnConflict()
                .DoUpdateSet(u.Name == Excluded(u.Name)),
            transaction);

        string name = connection
            .Query<string>(Select(u.Name).From(u).Where(u.Id == 1), transaction)
            .Single();

        Assert.Equal("AliceUpdated", name);
        transaction.Rollback();
    }

    // Live twins of the duplicate-list guards (guards-and-empty-states.md): the
    // guards reject the shape everywhere, so each lane pins what its engine does.
    [Fact]
    public void DuplicateInsertColumn_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "INSERT INTO users (id, id) VALUES (901, 902)",
            transaction: transaction);
        int count = connection
            .Query<int>(
                "SELECT COUNT(*) FROM users WHERE id IN (901, 902)",
                transaction: transaction)
            .Single();

        Assert.Equal(1, count); // one row lands, under whichever value the engine keeps
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
    public void DuplicateOnConflictTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "INSERT INTO users (id) VALUES (901) ON CONFLICT (id, id) DO NOTHING",
                transaction: transaction));
        transaction.Rollback();
    }

    [Fact]
    public void DuplicateUsingColumn_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Execute("SELECT id FROM users JOIN orders USING (id, id)");
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

    // The live twin of the re-listed joined-UPDATE guard off SQL Server (ADR 0011):
    // the T-SQL lead form names the alias alone, which SQLite reads as a table.
    [Fact]
    public void JoinedUpdateRelistedTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute(
                "UPDATE \"u\" SET age = 1 FROM users AS \"u\" "
                    + "INNER JOIN orders AS \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    // The live twins of the ORDER BY ordinal guards: SQLite reads both literals
    // as positions and rejects each, while the window position takes them.
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

    // The nested twin: a subquery resolves its own ordinals, which is why the
    // guard runs on every query block rather than the outermost one.
    [Fact]
    public void OrderByNegativeOrdinalInSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<DbException>(() =>
            connection.Execute("SELECT id FROM (SELECT id FROM users ORDER BY -1)"));
    }

    [Fact]
    public void OrderByNegativeOrdinalInWindow_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<long>("SELECT ROW_NUMBER() OVER (ORDER BY -1) FROM users").ToList();
    }

    // ADR 0012 non-goal (#523): the second accepting engine for the negative
    // LAG offset — SQLite reads it as a LEAD, exactly as PostgreSQL does.
    [Fact]
    public void LagNegativeOffset_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<int>("SELECT LEAD(id, 1) OVER (ORDER BY id) FROM users"),
            connection.ExecuteScalar<int>("SELECT LAG(id, -1) OVER (ORDER BY id) FROM users"));
    }

    // ADR 0012 non-goal (#523): SQLite's LIMIT -1 means "no limit", which is
    // why the row-count family cannot be guarded as universally invalid.
    [Fact]
    public void NegativeLimitCount_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<long>("SELECT COUNT(*) FROM users"),
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM (SELECT id FROM users LIMIT -1)"));
    }

    // The acceptance half of #532's OFFSET decision: SQLite reads a negative
    // offset as 0 rather than rejecting it, so no cell could report here.
    [Fact]
    public void NegativeOffset_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.Equal(
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM (SELECT id FROM users LIMIT 10 OFFSET 0)"),
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM (SELECT id FROM users LIMIT 10 OFFSET -1)"));
    }

    // ADR 0011: the acceptance that keeps the non-integer sort-key guard off
    // SQLite.
    [Fact]
    public void OrderByNonIntegerConstant_IsAcceptedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT id, name FROM users ORDER BY 2.5");
    }

    // #523: SQLA0102's live proofs. SQLite owns the FROM-form UPDATE
    // (JoinedUpdateFrom_Executes above) but neither joined spelling.
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
}
