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
        // Returning().Build() routes through BuildWithPart, which appends its
        // extra part directly (bypassing AddPart) before delegating to
        // BuildCore — a distinct path from every other stage method.
        IReturningBuilder ret = Update(_t).Set(_t.Code == 1).Returning(_t.Code);
        ret.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ret.Build(Dbms.Oracle));

        Assert.Equal(
            "This UPDATE statement was already built; start a new chain.",
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
            "This UPDATE statement was already built; start a new chain.",
            ex.Message);
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
}
