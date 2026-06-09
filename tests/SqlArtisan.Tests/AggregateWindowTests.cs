using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class AggregateWindowTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Sum_OverEmpty_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER ()";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over()).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Sum_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Sum_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Sum_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Count_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT COUNT(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Count(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Avg_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT AVG(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Avg(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Max_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT MAX(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Max(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Min_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT MIN(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Min(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
