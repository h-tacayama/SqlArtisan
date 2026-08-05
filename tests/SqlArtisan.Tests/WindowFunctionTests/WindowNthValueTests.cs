using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowNthValueTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void NthValue_WithoutOver_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Select(NthValue(_t.Name, 2)).Build());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NthValue_NonPositivePosition_ThrowsArgumentException(int position)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => NthValue(_t.Code, position));

        Assert.Equal("NTH_VALUE requires a positive position.", ex.Message);
    }

    [Fact]
    public void NthValue_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT NTH_VALUE(code, 2) OVER (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(NthValue(_t.Code, 2).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void NthValue_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT NTH_VALUE(code, 2) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(
                NthValue(_t.Code, 2).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void NthValue_OverFrameBetween_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT NTH_VALUE(code, 2) OVER (ORDER BY code ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)";

        // Act
        SqlStatement sql =
            Select(
                NthValue(_t.Code, 2).Over(
                    OrderBy(_t.Code).RowsBetween(UnboundedPreceding, UnboundedFollowing)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
