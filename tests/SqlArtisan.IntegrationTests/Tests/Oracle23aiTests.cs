using System.Data;
using System.Data.Common;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// Engine facts read at Oracle 23ai: those 23ai changed from the 21c baseline lane, each the
/// live gate for a doc claim whose rejecting half the 21c lane (<see cref="OracleTests"/>)
/// asserts, and others re-checked at 23ai.
/// </summary>
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public Oracle23aiTests(Oracle23aiFixture fixture)
    {
        _fixture = fixture;
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

    // #684: 23ai groups by a constant key as 21c does (OracleTests'
    // GroupByConstant_GroupsByTheConstant), with the setting at its default.
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

    // Only group_by_position_enabled makes 23ai read the position, the setting
    // docs/analyzer.md names beside SQLA0104's Oracle cell.
    [Fact]
    public void GroupByOrdinal_IsReadOnlyWithThePositionSetting()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        const string ByPosition = "SELECT department_id, COUNT(*) FROM users GROUP BY 1";

        Assert.Equal(
            "FALSE",
            connection.ExecuteScalar<string>(
                "SELECT value FROM v$parameter WHERE name = 'group_by_position_enabled'"));
        Assert.ThrowsAny<DbException>(() => connection.Query(ByPosition).ToList());

        connection.Execute("ALTER SESSION SET group_by_position_enabled = TRUE");
        try
        {
            Assert.Equal(
                connection.ExecuteScalar<long>(
                    "SELECT COUNT(DISTINCT department_id) FROM users"),
                connection.Query(ByPosition).Count());
        }
        finally
        {
            connection.Execute("ALTER SESSION SET group_by_position_enabled = FALSE");
        }
    }

    // The accepting half of the query-statements doc note: 23ai added the
    // multi-row VALUES table value constructor.
    [Fact]
    public void MultiRowValues_Executes()
    {
        UsersTable u = new();
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertInto(u, u.Id, u.Name).Values(201, "A").Values(202, "B"),
            transaction);

        long inserted = Convert.ToInt64(connection.ExecuteScalar(
            Select(Count(u.Id)).From(u).Where(u.Id.In(201, 202)), transaction));

        Assert.Equal(2, inserted);
        transaction.Rollback();
    }

    // The 23ai half of LockWaitGuard's live-verified claim (ADR 0012); the 21c
    // half is OracleTests.Wait_NegativeSeconds_Rejected.
    [Fact]
    public void Wait_NegativeSeconds_Rejected()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 3");
        connection.ExecuteScalar("SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT 0");

        Assert.ThrowsAny<Exception>(() => connection.ExecuteScalar(
            "SELECT age FROM users WHERE id = 1 FOR UPDATE WAIT -1"));
    }

    // The 21c lane's leading-WITH-before-MERGE rejection, re-run at 23ai: the
    // subquery-factoring clause is still no part of the MERGE grammar.
    [Fact]
    public void LeadingWithBeforeMerge_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "MERGE INTO users t USING (SELECT 1 AS id, 'x' AS name FROM dual) s "
                + "ON (t.id = s.id) WHEN MATCHED THEN UPDATE SET t.name = s.name",
            transaction: transaction);

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "WITH c AS (SELECT 1 AS id, 'x' AS name FROM dual) "
                    + "MERGE INTO users t USING c s ON (t.id = s.id) "
                    + "WHEN MATCHED THEN UPDATE SET t.name = s.name",
                transaction: transaction));
        transaction.Rollback();
    }

    // #590: Oracle Free 23ai takes FOR UPDATE only in a top-level SELECT; each unlocked control
    // runs, so the lock is what the engine rejects.
    [Fact]
    public void LockedSubqueryInIn_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id IN (SELECT user_id FROM orders FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedExistsSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users u WHERE EXISTS "
            + "(SELECT 1 FROM orders o WHERE o.user_id = u.id FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT id FROM users WHERE id = (SELECT user_id FROM orders WHERE id = 1)").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT id FROM users WHERE id = "
            + "(SELECT user_id FROM orders WHERE id = 1 FOR UPDATE)").ToList());
    }

    [Fact]
    public void LockedSelectedScalarSubquery_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id, (SELECT o.id FROM users o WHERE o.id = u.id) FROM users u").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
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
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT u.id FROM users u "
            + "CROSS APPLY (SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    [Fact]
    public void LockedLateralInlineView_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id) x").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT u.id FROM users u CROSS JOIN LATERAL "
            + "(SELECT o.id FROM users o WHERE o.id = u.id FOR UPDATE) x").ToList());
    }

    [Fact]
    public void LockedCteBody_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "WITH c AS (SELECT id FROM users) SELECT id FROM c").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "WITH c AS (SELECT id FROM users FOR UPDATE) SELECT id FROM c").ToList());
    }

    [Fact]
    public void LockedDerivedTable_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();

        connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users) d").ToList();
        Assert.ThrowsAny<Exception>(() => connection.Query<int>(
            "SELECT d.id FROM (SELECT id FROM users FOR UPDATE) d").ToList());
    }

    // #582: the acceptance twin behind SQLA0102's Oracle 23 floor for the FROM form,
    // as SqlArtisan emits it. orders.id is unique, so no row is updated twice.
    [Fact]
    public void JoinedUpdateFrom_Executes()
    {
        UsersTable u = new("u");
        OrdersTable o = new("o");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            Update(u).Set(u.Age == 999).From(o).Where(u.Id == o.Id),
            transaction);
        transaction.Rollback();
    }

    // #582: 23ai added UPDATE ... FROM, but the joined forms SqlArtisan withholds
    // RETURNING from stay rejected; the FROM control, led by the table name, runs.
    // Its source is one row per user: ORA-30926 rejects a row updated twice.
    [Fact]
    public void JoinedUpdateRelistedTarget_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            "UPDATE users \"u\" SET age = 1 FROM (SELECT DISTINCT user_id FROM orders) \"o\" "
                + "WHERE \"o\".user_id = \"u\".id",
            transaction: transaction);
        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "UPDATE \"u\" SET age = 1 FROM users \"u\" "
                    + "INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    // The re-listed lead, in FROM's list or through a join (#679), is ORA-00942 on
    // 23.26.3, where the lead that does not re-list runs (DeleteFromNotRelisted_Executes).
    [Fact]
    public void JoinedDeleteLead_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "DELETE \"u\" FROM users \"u\" "
                    + "INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "DELETE \"u\" FROM orders \"o\" "
                    + "INNER JOIN users \"u\" ON \"o\".user_id = \"u\".id",
                transaction: transaction));
        transaction.Rollback();
    }

    // #582: the acceptance twin behind SQLA0102's Oracle 23 floor for DELETE ... USING,
    // as SqlArtisan emits it. Alice's two orders go and the other three stay, so the
    // join filters as PostgreSQL's does rather than deleting every row.
    [Fact]
    public void DeleteUsing_Executes()
    {
        OrdersTable o = new("o");
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        int deleted = connection.Execute(
            DeleteFrom(o).Using(u).Where((o.UserId == u.Id) & (u.Name == "Alice")),
            transaction);
        int[] remaining =
        [
            .. connection.Query<int>(
                "SELECT id FROM orders ORDER BY id",
                transaction: transaction),
        ];

        Assert.Equal(2, deleted);
        Assert.Equal([3, 4, 5], remaining);
        transaction.Rollback();
    }

    // #679: 23.26.3 takes DELETE's FROM opener as it takes USING, so the joined
    // DELETE ... FROM that does not re-list its target builds on Oracle and runs.
    [Fact]
    public void DeleteFromNotRelisted_Executes()
    {
        OrdersTable o = new("o");
        UsersTable u = new("u");
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        int deleted = connection.Execute(
            DeleteFrom(o).From(u).Where((o.UserId == u.Id) & (u.Name == "Alice")),
            transaction);
        int[] remaining =
        [
            .. connection.Query<int>(
                "SELECT id FROM orders ORDER BY id",
                transaction: transaction),
        ];

        Assert.Equal(2, deleted);
        Assert.Equal([3, 4, 5], remaining);
        transaction.Rollback();
    }

    // #679: the joined forms 23.26.3 runs take RETURNING ... INTO too, reached on
    // DELETE ... FROM by a held stage, since the typestate offers it after Using(...).
    [Fact]
    public void JoinedDmlNotRelisted_ReturningInto_BindsOutputParameter()
    {
        OrdersTable o = new("o");
        UsersTable u = new("u");
        IDeleteBuilderDeleteOutput deleteFrom = DeleteFrom(o);
        deleteFrom.From(u).Where((o.UserId == u.Id) & (u.Id == 2));
        ISqlBuilder[] statements =
        [
            deleteFrom.Returning(o.Id).Into(new OutputParameter("rid", DbType.Int32)),
            DeleteFrom(o).Using(u).Where((o.UserId == u.Id) & (u.Id == 2))
                .Returning(o.Id).Into(new OutputParameter("rid", DbType.Int32)),
            Update(o).Set(o.Amount == 7).From(u).Where((o.UserId == u.Id) & (u.Id == 2))
                .Returning(o.Id).Into(new OutputParameter("rid", DbType.Int32)),
        ];

        foreach (ISqlBuilder statement in statements)
        {
            using IDbConnection connection = _fixture.OpenConnection();
            using IDbTransaction transaction = connection.BeginTransaction();

            DynamicParameters outputs = connection.ExecuteReturningInto(statement, transaction);

            Assert.Equal(3, Convert.ToInt32(outputs.Get<object>("rid")!.ToString()));
            transaction.Rollback();
        }
    }

    [Fact]
    public void JoinedUpdateJoinForm_IsRejectedByTheEngine()
    {
        using IDbConnection connection = _fixture.OpenConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        Assert.ThrowsAny<Exception>(() =>
            connection.Execute(
                "UPDATE users \"u\" INNER JOIN orders \"o\" ON \"o\".user_id = \"u\".id "
                    + "SET \"u\".age = 1",
                transaction: transaction));
        transaction.Rollback();
    }

    // The 21c lane's #662 DISTINCT-window facts, re-run at 23ai: whether the engine
    // runs a window on a DISTINCT aggregate decides whether the library may reject the
    // pairing as valid on no dialect (ADR 0007).
    [Theory]
    [MemberData(
        nameof(DistinctAggregateWindows.OrderedOnly),
        MemberType = typeof(DistinctAggregateWindows))]
    public void DistinctAggregateOrderedWindow_IsRejectedByTheEngine(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();

        Assert.ThrowsAny<System.Data.Common.DbException>(() => connection.Execute(sql));
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
}
