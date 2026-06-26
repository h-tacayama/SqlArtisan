using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowLastValueTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void LastValue_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LAST_VALUE(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(LastValue(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void LastValue_OverFrameBetween_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT LAST_VALUE(code) OVER (ORDER BY code ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)";

        // Act
        SqlStatement sql =
            Select(
                LastValue(_t.Code).Over(
                    OrderBy(_t.Code).RowsBetween(UnboundedPreceding, UnboundedFollowing)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
