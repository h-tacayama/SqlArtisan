using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The single-use builder contract (#245): once Build() succeeds, any further
// stage call or Build() on that instance throws instead of silently
// contaminating the next build.
public class BuilderReuseTests
{
    private const string SelectBuiltMessage =
        "This SELECT statement was already built; start a new chain.";

    private readonly TestTable _t = new();

    [Fact]
    public void Pagination_ChainedAfterBuild_ThrowsArgumentException()
    {
        // The audit's A9 shape: a held chain built once, then extended down the
        // other pagination family — previously stacked into invalid SQL.
        var stmt = Select(_t.Code).From(_t).OrderBy(_t.Code);
        stmt.Limit(10).Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.OffsetRows(20));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Where_ChainedAfterBuild_ThrowsArgumentException()
    {
        var q = Select(_t.Code).From(_t);
        q.Where(_t.Code == 1).Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            q.Where(_t.Name == "a"));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Build_CalledTwice_ThrowsArgumentException()
    {
        var stmt = Select(_t.Code).From(_t).Where(_t.Code == 1);
        stmt.Build();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            stmt.Build(Dbms.Oracle));

        Assert.Equal(SelectBuiltMessage, ex.Message);
    }

    [Fact]
    public void Build_ThrewOnDialectGuard_LeavesBuilderUsable()
    {
        // A throwing Build() must not freeze the builder — the SQL Server aliased
        // target is rejected, but the same instance still builds for a dialect
        // that allows it.
        TestTable aliased = new("t");
        var stmt = DeleteFrom(aliased);

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
        // The supported way to emit one query for several dialects: rebuild the
        // chain from a factory rather than reusing a held instance.
        Func<ISqlBuilder> query = () =>
            Select(_t.Code).From(_t).Where(_t.Code == 1);

        SqlStatement pg = query().Build(Dbms.PostgreSql);
        SqlStatement ora = query().Build(Dbms.Oracle);

        Assert.Equal("SELECT code FROM test_table WHERE code = :0", pg.Text);
        Assert.Equal("SELECT code FROM test_table WHERE code = :0", ora.Text);
    }

    [Fact]
    public void Update_ChainedAfterBuild_ThrowsArgumentException()
    {
        var stmt = Update(_t).Set(_t.Code == 1);
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
        var stmt = InsertInto(_t, _t.Code, _t.Name).Values(1, "a");
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
