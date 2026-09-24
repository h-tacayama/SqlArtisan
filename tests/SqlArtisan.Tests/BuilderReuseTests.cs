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

        ArgumentException ex = Assert.Throws<ArgumentException>(() => stmt.Build(Dbms.SqlServer));
        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE "
                + "statement; use an unaliased target table — "
                + "a correlated UPDATE or DELETE joins "
                + "through From(...) instead.",
            ex.Message);

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
        Assert.Equal(1, pg.Parameters.Get<int>(":0"));
        Assert.Equal(1, ora.Parameters.Get<int>(":0"));
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
        // The ordering #245's freeze missed: Into() hands the chain to the inner
        // builder, so a later Build() on the held stage appended a second RETURNING.
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
    public void Where_CalledTwiceOnHeldSubquery_ThrowsArgumentException()
    {
        // A nested render never passes through BuildCore, so the walk must run
        // from Format too — the duplicate is no less wrong one level down.
        TestTable r = new("r");
        var held = Select(r.Code).From(r);
        held.Where(r.Code == 1);
        held.Where(r.Name == "x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).Where(_t.Code.In(held)).Build());

        Assert.Equal(
            "A statement takes at most one WHERE clause per query block; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void Join_NoOnOnHeldSubquery_ThrowsArgumentException()
    {
        TestTable r = new("r");
        TestTable s = new("s");
        var held = Select(r.Code).From(r);
        held.InnerJoin(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).Where(_t.Code.In(held)).Build());

        Assert.Equal(
            "A join is missing its ON or USING clause; the statement was built "
                + "from a held builder before the join was completed.",
            ex.Message);
    }

    [Fact]
    public void Join_NoOnOnHeldCteBody_ThrowsArgumentException()
    {
        TestTable r = new("r");
        TestTable s = new("s");
        Cte cte = new("cte");
        var held = Select(r.Code).From(r);
        held.InnerJoin(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(held)).Select(cte.Column("code")).From(cte).Build());

        Assert.Equal(
            "A join is missing its ON or USING clause; the statement was built "
                + "from a held builder before the join was completed.",
            ex.Message);
    }

    [Fact]
    public void Where_CalledTwiceOnHeldScalarSubquery_ThrowsArgumentException()
    {
        TestTable r = new("r");
        var held = Select(Count(Asterisk)).From(r);
        held.Where(r.Code == 1);
        held.Where(r.Name == "x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code, held).From(_t).Build());

        Assert.Equal(
            "A statement takes at most one WHERE clause per query block; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void With_LeadingAndFeedingSelect_CorrectSql()
    {
        // INSERT ... WITH ... SELECT: the feeding SELECT is its own query block
        // for WITH, so the leading and the mid-chain With(...) legally coexist.
        TestTable a = new("a");
        Cte c1 = new("c1");
        Cte c2 = new("c2");

        SqlStatement sql =
            With(c1.As(Select(a.Code).From(a)))
            .InsertInto(_t, _t.Code)
            .With(c2.As(Select(c1.Column("code")).From(c1)))
            .Select(c2.Column("code"))
            .From(c2)
            .Build();

        Assert.Equal(
            "WITH \"c1\" AS (SELECT \"a\".code FROM test_table \"a\") "
                + "INSERT INTO test_table (code) "
                + "WITH \"c2\" AS (SELECT \"c1\".code FROM \"c1\") "
                + "SELECT \"c2\".code FROM \"c2\"",
            sql.Text);
    }

    [Fact]
    public void With_CalledTwiceOnHeldInsertStage_ThrowsArgumentException()
    {
        TestTable a = new("a");
        Cte c1 = new("c1");
        Cte c2 = new("c2");
        IInsertBuilderColumns held = InsertInto(_t, _t.Code);
        held.With(c1.As(Select(a.Code).From(a)));
        held.With(c2.As(Select(a.Code).From(a)));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            held.Select(c2.Column("code")).From(c2).Build());

        Assert.Equal(
            "A statement takes at most one WITH clause per query block; "
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
            Update(_t).Set(_t.Code == 1).Returning(_t.Code).Into(
                new OutputParameter("out", DbType.Int32));
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
        // A failed delegated build must not freeze the stage, or the retry
        // reports a false "already built" instead of the real cause.
        TestTable aliased = new("t");
        IReturningBuilder stage = Update(aliased).Set(aliased.Code == 1).Returning(aliased.Code);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stage.Build(Dbms.SqlServer));
        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE "
                + "statement; use an unaliased target table — "
                + "a correlated UPDATE or DELETE joins "
                + "through From(...) instead.",
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
        // A failed batch must leave no partial rows behind, or the corrected
        // retry silently inserts the survivors twice.
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
        ArgumentException ex = Assert.Throws<ArgumentException>(() => stage.OnDuplicateKeyUpdate());
        Assert.Equal("ON DUPLICATE KEY UPDATE requires at least one assignment.", ex.Message);

        SqlStatement sql = stage.OnDuplicateKeyUpdate(_t.Code == 5).Build(Dbms.MySql);

        Assert.Equal(
            "INSERT INTO test_table (code) VALUES (?0) AS new "
                + "ON DUPLICATE KEY UPDATE code = ?1",
            sql.Text);
    }

    // ── exclusive spellings of one slot ────────────────────────────────

    [Fact]
    public void Limit_ThenFetchFirst_OnHeldStage_ThrowsArgumentException()
    {
        // Distinct clause kinds, so the once-per-kind walk alone let both stack.
        var held = Select(_t.Code).From(_t).Where(_t.Code > 1);
        held.Limit(3);
        held.FetchFirst(5);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(
            "FETCH cannot be combined with LIMIT in one query block; "
                + "a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void FetchFirst_ThenLimit_OnHeldStage_ThrowsArgumentException()
    {
        var held = Select(_t.Code).From(_t).Where(_t.Code > 1);
        held.FetchFirst(5);
        held.Limit(3);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(
            "LIMIT cannot be combined with FETCH in one query block; "
                + "a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void Values_ThenSelect_OnHeldInsertStage_ThrowsArgumentException()
    {
        TestTable s = new("s");
        IInsertBuilderColumns held = InsertInto(_t, _t.Code);
        var values = held.Values(1);
        held.Select(s.Code).From(s);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => values.Build());

        Assert.Equal(
            "An INSERT takes one row source — VALUES, SET, or SELECT; "
                + "a stage on a held builder supplied a second.",
            ex.Message);
    }

    [Fact]
    public void Set_ThenValues_OnHeldInsertStage_ThrowsArgumentException()
    {
        // SET renders as a VALUES row, so the pair is a duplicate row source.
        IInsertBuilderTable held = InsertInto(_t);
        var set = held.Set(_t.Code == 1);
        held.Values(2);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => set.Build());

        Assert.Equal(
            "An INSERT takes one row source — VALUES, SET, or SELECT; "
                + "a stage on a held builder supplied a second.",
            ex.Message);
    }

    [Fact]
    public void InsertSelect_UnionSelect_CorrectSql()
    {
        // The legal twin: a set operator opens the feeding SELECT's next block,
        // so its second SELECT is not a second row source.
        TestTable s = new("s");
        TestTable r = new("r");

        SqlStatement sql =
            InsertInto(_t, _t.Code)
            .Select(s.Code).From(s)
            .Union
            .Select(r.Code).From(r)
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code) "
                + "SELECT \"s\".code FROM test_table \"s\" "
                + "UNION "
                + "SELECT \"r\".code FROM test_table \"r\"",
            sql.Text);
    }

    [Fact]
    public void From_ThenUsing_OnHeldDeleteStage_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable o = new("o");
        IDeleteBuilderDelete held = DeleteFrom(t);
        held.From(t, o);
        held.Using(o);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(
            "USING cannot be combined with FROM in one query block; "
                + "a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void On_AfterCrossJoinOnHeldJoinStage_ThrowsArgumentException()
    {
        // A condition-free join consumes the ON slot instead of re-admitting
        // one: `CROSS JOIN ... ON` runs on SQLite with a different row set.
        TestTable s = new("s");
        TestTable x = new("x");
        ISelectBuilderJoin held = Select(_t.Code).From(_t).InnerJoin(s);
        ISelectBuilderFrom joined = held.On(_t.Code == s.Code);
        joined.CrossJoin(x);
        held.On(_t.Name == s.Name);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => joined.Build());

        Assert.Equal(
            "A join takes at most one ON or USING clause; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void CrossJoin_ThenInnerJoinOn_CorrectSql()
    {
        // The legal twin: the conditioned join after a CROSS JOIN takes its ON.
        TestTable s = new("s");
        TestTable x = new("x");

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .CrossJoin(x)
            .InnerJoin(s).On(_t.Code == s.Code)
            .Build();

        Assert.Equal(
            "SELECT code FROM test_table "
                + "CROSS JOIN test_table \"x\" "
                + "INNER JOIN test_table \"s\" ON code = \"s\".code",
            sql.Text);
    }

    [Fact]
    public void OnConflict_ThenOnDuplicateKeyUpdate_OnHeldStage_ThrowsArgumentException()
    {
        IInsertBuilderValues held = InsertInto(_t, _t.Code).Values(1);
        held.OnConflict(_t.Code).DoNothing();
        ISqlBuilder second = held.OnDuplicateKeyUpdate(_t.Name == "y");

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build(Dbms.MySql));

        Assert.Equal(
            "ON DUPLICATE KEY UPDATE cannot be combined with ON CONFLICT in one query block; "
                + "a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void DoNothing_ThenDoUpdateSet_OnHeldStage_ThrowsArgumentException()
    {
        IInsertBuilderOnConflict held = InsertInto(_t, _t.Code).Values(1).OnConflict(_t.Code);
        held.DoNothing();
        ISqlBuilder second = held.DoUpdateSet(_t.Name == "y");

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build());

        Assert.Equal(
            "DO UPDATE SET cannot be combined with DO NOTHING in one query block; "
                + "a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void ThenUpdateSet_ThenThenDelete_OnHeldBranch_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        IMergeBuilderWhenMatched held = MergeInto(t).Using(s).On(t.Code == s.Code).WhenMatched();
        held.ThenUpdateSet(t.Name == s.Name);
        ISqlBuilder second = held.ThenDelete();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build());

        Assert.Equal(
            "A MERGE WHEN branch takes one action; DELETE cannot be combined with UPDATE SET, "
                + "and a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void UpdateWhere_Twice_OnHeldBranch_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        IMergeBuilderThenUpdateSet held =
            MergeInto(t).Using(s).On(t.Code == s.Code)
            .WhenMatched().ThenUpdateSet(t.Name == s.Name);
        held.UpdateWhere(t.Code == 1);
        ISqlBuilder second = held.UpdateWhere(t.Code == 2);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build(Dbms.Oracle));

        Assert.Equal(
            "A MERGE WHEN branch takes at most one UPDATE WHERE clause; "
                + "a stage on a held builder was called twice.",
            ex.Message);
    }

    [Fact]
    public void InsertWhere_AfterTheNextBranch_OnHeldStage_ThrowsArgumentException()
    {
        // The held INSERT's filter lands in the UPDATE branch that followed it.
        TestTable t = new("t");
        TestTable s = new("s");
        IMergeBuilderValues held =
            MergeInto(t).Using(s).On(t.Code == s.Code)
            .WhenNotMatched().ThenInsert(t.Code, t.Name).Values(s.Code, s.Name);
        held.WhenMatched().ThenUpdateSet(t.Name == s.Name);
        ISqlBuilder second = held.InsertWhere(s.Code > 0);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build(Dbms.Oracle));

        Assert.Equal(
            "A MERGE WHEN branch takes one action; INSERT WHERE cannot be combined with "
                + "UPDATE SET, and a stage on a held builder supplied both.",
            ex.Message);
    }

    [Fact]
    public void ThenUpdateSet_ThenDeleteWhere_CorrectSql()
    {
        // The legal twin: Oracle's UPDATE SET ... DELETE WHERE pairs two actions.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            MergeInto(t).Using(s).On(t.Code == s.Code)
            .WhenMatched().ThenUpdateSet(t.Name == s.Name).DeleteWhere(t.Code == 0)
            .Build(Dbms.Oracle);

        Assert.Equal(
            "MERGE INTO test_table \"t\" USING test_table \"s\" ON (\"t\".code = \"s\".code) "
                + "WHEN MATCHED THEN UPDATE SET name = \"s\".name DELETE WHERE \"t\".code = :0",
            sql.Text);
    }

    [Fact]
    public void Union_WithoutSelect_OnHeldStage_ThrowsArgumentException()
    {
        var held = Select(_t.Code).From(_t);
        _ = held.Union;

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.Build());

        Assert.Equal(
            "A set operator is missing its SELECT; the statement was built "
                + "from a held builder before the operator was completed.",
            ex.Message);
    }

    [Fact]
    public void Union_ThenWhere_OnHeldStage_ThrowsArgumentException()
    {
        var held = Select(_t.Code).From(_t);
        _ = held.Union;
        var second = held.Where(_t.Code == 1);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => second.Build());

        Assert.Equal(
            "A set operator is missing its SELECT; the statement was built "
                + "from a held builder before the operator was completed.",
            ex.Message);
    }

    [Fact]
    public void Union_WithoutSelect_OnHeldSubquery_ThrowsArgumentException()
    {
        TestTable r = new("r");
        var held = Select(r.Code).From(r);
        _ = held.Union;

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).Where(_t.Code.In(held)).Build());

        Assert.Equal(
            "A set operator is missing its SELECT; the statement was built "
                + "from a held builder before the operator was completed.",
            ex.Message);
    }

    [Fact]
    public void Returning_ThenInnerBuild_ThrowsArgumentException()
    {
        // The RETURNING stage appends its clause only when it builds, so a build
        // from the earlier stage would drop the clause the caller wrote.
        IInsertBuilderValues inner = InsertInto(_t, _t.Code).Values(1);
        IReturningBuilder returning = inner.Returning(_t.Code);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => inner.Build());

        Assert.Equal(
            "A RETURNING clause was written on this statement, but it was built from "
                + "the stage before it; build from the RETURNING stage.",
            ex.Message);
        Assert.Equal(
            "INSERT INTO test_table (code) VALUES (:0) RETURNING code",
            returning.Build().Text);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("delete")]
    public void Returning_ThenInnerBuild_UpdateAndDelete_ThrowsArgumentException(string kind)
    {
        ISqlBuilder inner = kind == "update"
            ? Update(_t).Set(_t.Code == 1)
            : DeleteFrom(_t);
        ((IReturning)inner).Returning(_t.Code);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => inner.Build());

        Assert.Equal(
            "A RETURNING clause was written on this statement, but it was built from "
                + "the stage before it; build from the RETURNING stage.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_ThenInnerBuild_CorrectSql()
    {
        // The legal twin: Into(...) appends its clause, so the inner build carries it.
        IInsertBuilderValues inner = InsertInto(_t, _t.Code).Values(1);
        inner.Returning(_t.Code).Into(new OutputParameter("out_code", DbType.Int32));

        Assert.Equal(
            "INSERT INTO test_table (code) VALUES (:0) RETURNING code INTO :out_code",
            inner.Build(Dbms.Oracle).Text);
    }

    // The obligation survives a failed build from the RETURNING stage: a dialect
    // guard on the retry must not leave the earlier stage free to drop the clause.
    [Fact]
    public void Returning_BuildThrewOnDialectGuard_InnerBuildStillThrows()
    {
        TestTable aliased = new("t");
        IUpdateBuilderSet inner = Update(aliased).Set(aliased.Code == 1);
        IReturningBuilder stage = inner.Returning(aliased.Code);
        ArgumentException guard = Assert.Throws<ArgumentException>(
            () => stage.Build(Dbms.SqlServer));
        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE "
                + "statement; use an unaliased target table — "
                + "a correlated UPDATE or DELETE joins "
                + "through From(...) instead.",
            guard.Message);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => inner.Build(Dbms.PostgreSql));

        Assert.Equal(ReturningPendingMessage, ex.Message);
    }

    // A Returning(...) call its own guard rejects leaves the inner builder as it was.
    [Fact]
    public void Returning_ThrowingCall_LeavesInnerBuildUsable()
    {
        IUpdateBuilderSet inner = Update(_t).Set(_t.Code == 1);
        ArgumentException rejected =
            Assert.Throws<ArgumentException>(() => inner.Returning(new object()));
        Assert.Equal("Invalid type for SelectItem: System.Object", rejected.Message);

        SqlStatement sql = inner.Build();

        Assert.Equal("UPDATE test_table SET code = :0", sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    // Each written stage is its own obligation: Into on one does not discharge
    // the other, and neither stage may build past its sibling.
    [Fact]
    public void Returning_TwoStages_IntoOnOne_InnerBuildThrows()
    {
        IUpdateBuilderSet inner = Update(_t).Set(_t.Code == 1);
        IReturningBuilder first = inner.Returning(_t.Code);
        _ = inner.Returning(_t.Name);
        first.Into(new OutputParameter("out_code", DbType.Int32));

        ArgumentException ex = Assert.Throws<ArgumentException>(() => inner.Build(Dbms.Oracle));

        Assert.Equal(ReturningPendingMessage, ex.Message);
    }

    [Fact]
    public void Returning_TwoStages_BuildFromOne_ThrowsArgumentException()
    {
        IUpdateBuilderSet inner = Update(_t).Set(_t.Code == 1);
        IReturningBuilder first = inner.Returning(_t.Code);
        _ = inner.Returning(_t.Name);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => first.Build());

        Assert.Equal(
            "Two RETURNING stages were written on this statement; a build from either "
                + "would drop the other, so write RETURNING once.",
            ex.Message);
    }

    [Fact]
    public void Returning_AfterBuild_ThrowsArgumentException()
    {
        IUpdateBuilderSet inner = Update(_t).Set(_t.Code == 1);
        inner.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => inner.Returning(_t.Code));

        Assert.Equal("This UPDATE statement was already built; start a new chain.", ex.Message);
    }

    private const string ReturningPendingMessage =
        "A RETURNING clause was written on this statement, but it was built from the stage "
        + "before it; build from the RETURNING stage.";
}
