using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowLagTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Lag_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LAG(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Lag(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Lag_WithOffset_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LAG(code, 2) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Lag(_t.Code, 2).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Lag_WithOffsetAndDefault_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LAG(code, 1, :0) OVER (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Lag(_t.Code, 1, 0).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Lag_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT LAG(code) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Lag(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
