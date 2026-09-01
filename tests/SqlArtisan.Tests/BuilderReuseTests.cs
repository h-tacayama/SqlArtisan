using System.Data;
using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The single-use builder contract: once Build() succeeds, any further stage
// call or Build() on that instance throws instead of silently contaminating
// the next build.
public class BuilderReuseTests
{
    private const string SelectBuiltMessage =
        "This SELECT statement was already built; start a new chain.";

    private readonly TestTable _t = new();

    [Fact]
    public void Pagination_ChainedAfterBuild_ThrowsArgumentException()
    {
        // A held chain built once, then extended down the other pagination
        // family — previously stacked both families into invalid SQL.
        ISelectBuilderOrderBy stmt = Select(_t.Code).From(_t).OrderBy(_t.Code);
        stmt.Limit(10).Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.OffsetRows(20));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Where_ChainedAfterBuild_ThrowsArgumentException()
    {
        ISelectBuilderFrom q = Select(_t.Code).From(_t);
        q.Where(_t.Code == 1).Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            q.Where(_t.Name == "a"));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Build_CalledTwice_ThrowsArgumentException()
    {
        ISelectBuilderWhere stmt = Select(_t.Code).From(_t).Where(_t.Code == 1);
        stmt.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.Build(Dbms.Oracle));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Build_ThrewOnDialectGuard_LeavesBuilderUsable()
    {
        // A throwing Build() must not freeze the builder — a fix-up on the same
        // instance still builds.
        TestTable aliased = new("t");
        IDeleteBuilderDelete stmt = DeleteFrom(aliased);

        Assert.Throws<ArgumentException>(() => stmt.Build(Dbms.SqlServer));

        SqlStatement sql = stmt.Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Build_FreshChainPerDialect_BuildsEach()
    {
        // A local function parameterized by the part that changes — here, the
        // dialect — rebuilds the chain per call instead of reusing an instance.
        SqlStatement Query(Dbms dbms) =>
            Select(_t.Code).From(_t).Where(_t.Code == 1).Build(dbms);

        SqlStatement pg = Query(Dbms.PostgreSql);
        SqlStatement ora = Query(Dbms.Oracle);

        Assert.Equal("SELECT code FROM test_table WHERE code = :0", pg.Text);
        Assert.Equal("SELECT code FROM test_table WHERE code = :0", ora.Text);
    }

    [Fact]
    public void Returning_BuildCalledTwice_ThrowsArgumentException()
    {
        // Returning().Build() routes through BuildWithPart, which bypasses
        // AddPart's guard directly — a distinct path from every other stage.
        IReturningBuilder ret = Update(_t).Set(_t.Code == 1).Returning(_t.Code);
        ret.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ret.Build(Dbms.Oracle));

        Assert.Equal(
            "This RETURNING clause was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_ChainedAfterBuild_ThrowsArgumentException()
    {
        IReturningBuilder ret = Update(_t).Set(_t.Code == 1).Returning(_t.Code);
        ret.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ret.Into(new OutputParameter("out", DbType.Int32)));

        Assert.Equal(
            "This RETURNING clause was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void Returning_BuildAfterInto_ThrowsArgumentException()
    {
        // The one ordering #245's freeze missed: Into() hands the chain to the
        // inner builder, and a later Build() on the held RETURNING stage would
        // have appended a second RETURNING clause.
        IReturningBuilder ret = Update(_t).Set(_t.Code == 1).Returning(_t.Code);
        ret.Into(new OutputParameter("out", DbType.Int32));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ret.Build(Dbms.Oracle));

        Assert.Equal(
            "This RETURNING clause was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_CalledTwice_ThrowsArgumentException()
    {
        IReturningBuilder ret = Update(_t).Set(_t.Code == 1).Returning(_t.Code);
        ret.Into(new OutputParameter("out", DbType.Int32));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ret.Into(new OutputParameter("out2", DbType.Int32)));

        Assert.Equal(
            "This RETURNING clause was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void Where_CalledTwiceOnHeldStage_ThrowsArgumentException()
    {
        var held = Select(_t.Code).From(_t);
        held.Where(_t.Code == 1);
        held.Where(_t.Name == "x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(
            "A statement takes at most one WHERE clause per query block; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void Where_OncePerCompoundQueryBlock_CorrectSql()
    {
        // The legal twin: a set operator starts a new query block, so each
        // branch carries its own WHERE.
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .Union
            .Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 2)
            .Build();

        Assert.Equal(
            "SELECT code FROM test_table WHERE code = :0 "
                + "UNION SELECT code FROM test_table WHERE code = :1",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void ReturningInto_BuildCalledTwice_ThrowsArgumentException()
    {
        ISqlBuilder withInto =
            Update(_t).Set(_t.Code == 1).Returning(_t.Code).Into(new OutputParameter("out", DbType.Int32));
        withInto.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            withInto.Build(Dbms.Oracle));

        Assert.Equal(
            "This UPDATE statement was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void Update_ChainedAfterBuild_ThrowsArgumentException()
    {
        IUpdateBuilderSet stmt = Update(_t).Set(_t.Code == 1);
        stmt.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.Where(_t.Name == "a"));

        Assert.Equal(
            "This UPDATE statement was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void InsertValues_ChainedAfterBuild_ThrowsArgumentException()
    {
        IInsertBuilderValues stmt = InsertInto(_t, _t.Code, _t.Name).Values(1, "a");
        stmt.Build();

        // A repeat Values() accumulates a row via AddRow, bypassing AddPart —
        // ThrowIfBuilt on Values guards that path too.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.Values(2, "b"));

        Assert.Equal(
            "This INSERT statement was already built; start a new chain.",
            ex.Message);
    }

    [Fact]
    public void Returning_BuildThrewOnDialectGuard_LeavesStageUsable()
    {
        // The RETURNING stage mirrors BuildCore's ordering: a failed delegated
        // build must not freeze the stage, or the retry reports a false
        // "already built" instead of the real cause.
        TestTable aliased = new("t");
        IReturningBuilder stage = Update(aliased).Set(aliased.Code == 1).Returning(aliased.Code);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stage.Build(Dbms.SqlServer));
        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, "
                + "or DELETE statement; use an unaliased target table.",
            ex.Message);

        SqlStatement sql = stage.Build(Dbms.PostgreSql);

        Assert.Equal(
            "UPDATE test_table AS \"t\" SET code = :0 RETURNING \"t\".code",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void InsertValues_BatchThrewOnWidthGuard_LeavesBuilderUsable()
    {
        // The batch overloads validate every row before appending any: a failed
        // batch must leave no partial rows behind, or the corrected retry would
        // silently insert the survivors twice.
        IInsertBuilderColumns stmt = InsertInto(_t, _t.Code, _t.Name);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.Values([[1, "a"], [2]]));
        Assert.Equal(
            "All rows in a multi-row INSERT must have the same number of values; "
                + "the first row has 2, but this row has 1.",
            ex.Message);

        SqlStatement sql = stmt.Values([[1, "a"], [2, "b"]]).Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1), (:2, :3)",
            sql.Text);
        Assert.Equal(4, sql.Parameters.Count);
    }

    [Fact]
    public void On_CalledTwiceOnHeldJoinStage_ThrowsArgumentException()
    {
        TestTable s = new("s");
        ISelectBuilderJoin held = Select(_t.Code).From(_t).InnerJoin(s);
        held.On(_t.Code == s.Code);
        ISelectBuilderFrom second = held.On(_t.Name == s.Name);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build());

        Assert.Equal(
            "A join takes at most one ON or USING clause; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void On_OncePerJoin_CorrectSql()
    {
        // The legal twin: each join clause re-admits one ON.
        TestTable s = new("s");
        TestTable u = new("u");

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .InnerJoin(s)
            .On(_t.Code == s.Code)
            .InnerJoin(u)
            .On(_t.Code == u.Code)
            .Build();

        Assert.Equal(
            "SELECT code FROM test_table "
                + "INNER JOIN test_table \"s\" ON code = \"s\".code "
                + "INNER JOIN test_table \"u\" ON code = \"u\".code",
            sql.Text);
    }

    private const string DanglingJoinMessage =
        "A join is missing its ON or USING clause; the statement was built "
            + "from a held builder before the join was completed.";

    [Fact]
    public void Join_NoOnOnHeldStage_ThrowsArgumentException()
    {
        // The compile-time pending type can be bypassed by building from a
        // held pre-join stage — the silent cartesian product ADR 0017 rejects.
        TestTable s = new("s");
        ISelectBuilderFrom held = Select(_t.Code).From(_t);
        held.InnerJoin(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(DanglingJoinMessage, ex.Message);
    }

    [Fact]
    public void Join_NoOnThenLaterStageOnHeldBuilder_ThrowsArgumentException()
    {
        TestTable s = new("s");
        ISelectBuilderFrom held = Select(_t.Code).From(_t);
        held.InnerJoin(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            held.Where(_t.Code == 1).Build());

        Assert.Equal(DanglingJoinMessage, ex.Message);
    }

    [Fact]
    public void UpdateJoin_NoOnOnHeldStage_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        IUpdateBuilderUpdate held = Update(t);
        held.InnerJoin(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            held.Set(t.Code == 1).Build(Dbms.MySql));

        Assert.Equal(DanglingJoinMessage, ex.Message);
    }

    [Fact]
    public void OnDuplicateKeyUpdate_ThrewOnEmptyAssignments_RetryEmitsSingleRowAlias()
    {
        // The failed call must leave nothing behind: appending the row alias
        // before parsing let a fix-up retry emit `AS new AS new`.
        IInsertBuilderValues stage = InsertInto(_t, _t.Code).Values(1);
        Assert.Throws<ArgumentException>(() => stage.OnDuplicateKeyUpdate());

        SqlStatement sql = stage.OnDuplicateKeyUpdate(_t.Code == 5).Build(Dbms.MySql);

        Assert.Equal(
            "INSERT INTO test_table (code) VALUES (?0) AS new "
                + "ON DUPLICATE KEY UPDATE code = ?1",
            sql.Text);
    }
}
