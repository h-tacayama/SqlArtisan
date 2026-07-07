using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// agg(...) FILTER (WHERE cond) conditional aggregation (#154). Native on
// PostgreSQL / SQLite; emitted faithfully elsewhere.
public class AggregateFilterTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Filter_OnSum_CorrectSql()
    {
        SqlStatement sql = Select(Sum(_t.Code).Filter(_t.Code > 0)).Build();

        Assert.Equal("SELECT SUM(code) FILTER (WHERE code > :0)", sql.Text);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Filter_OnCount_CorrectSql()
    {
        SqlStatement sql = Select(Count(_t.Code).Filter(_t.Name == "x")).Build();

        Assert.Equal("SELECT COUNT(code) FILTER (WHERE name = :0)", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Filter_OnCountNoArgument_CorrectSql()
    {
        SqlStatement sql = Select(Count().Filter(_t.Name == "x")).Build();

        Assert.Equal("SELECT COUNT(*) FILTER (WHERE name = :0)", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Filter_WithDistinct_CorrectSql()
    {
        SqlStatement sql = Select(Sum(Distinct, _t.Code).Filter(_t.Code > 0)).Build();

        Assert.Equal("SELECT SUM(DISTINCT code) FILTER (WHERE code > :0)", sql.Text);
    }

    [Fact]
    public void Filter_WithOver_CorrectSql()
    {
        SqlStatement sql =
            Select(Sum(_t.Code).Filter(_t.Code > 0).Over(PartitionBy(_t.Name))).Build();

        Assert.Equal(
            "SELECT SUM(code) FILTER (WHERE code > :0) OVER (PARTITION BY name)",
            sql.Text);
    }
}
