using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowLastValueTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void LastValue_WithoutOver_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Select(LastValue(_t.Name)).Build());
    }

    [Fact]
    public void LastValue_OverOrderBy_CorrectSql()
    {
        string expected = "SELECT LAST_VALUE(code) OVER (ORDER BY code)";
        SqlStatement sql =
            Select(LastValue(_t.Code).Over(OrderBy(_t.Code))).Build();
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void LastValue_OverPartitionByOrderBy_CorrectSql()
    {
        string expected =
            "SELECT LAST_VALUE(code) OVER (PARTITION BY name ORDER BY code)";
        SqlStatement sql =
            Select(
                LastValue(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void LastValue_OverFrameBetween_CorrectSql()
    {
        string expected =
            "SELECT LAST_VALUE(code) OVER (ORDER BY code ROWS BETWEEN UNBOUNDED PRECEDING AND "
                + "UNBOUNDED FOLLOWING)";
        SqlStatement sql =
            Select(
                LastValue(_t.Code).Over(
                    OrderBy(_t.Code).RowsBetween(UnboundedPreceding, UnboundedFollowing)))
            .Build();
        Assert.Equal(expected, sql.Text);
    }
}
