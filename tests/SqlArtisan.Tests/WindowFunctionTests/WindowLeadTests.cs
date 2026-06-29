using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowLeadTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Lead_WithoutOver_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Select(Lead(_t.Name)).Build());
    }

    [Fact]
    public void Lead_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LEAD(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Lead(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Lead_WithOffset_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LEAD(code, 2) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Lead(_t.Code, 2).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Lead_WithOffsetAndDefault_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LEAD(code, 1, :0) OVER (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Lead(_t.Code, 1, 0).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Lead_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT LEAD(code) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Lead(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
