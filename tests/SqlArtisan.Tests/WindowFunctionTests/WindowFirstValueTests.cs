using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowFirstValueTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void FirstValue_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT FIRST_VALUE(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(FirstValue(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void FirstValue_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT FIRST_VALUE(code) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(
                FirstValue(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void FirstValue_OverFrame_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT FIRST_VALUE(code) OVER (ORDER BY code ROWS UNBOUNDED PRECEDING)";

        // Act
        SqlStatement sql =
            Select(
                FirstValue(_t.Code).Over(OrderBy(_t.Code).Rows(UnboundedPreceding)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
