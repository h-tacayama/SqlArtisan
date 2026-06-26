using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowNtileTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Ntile_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT NTILE(4) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Ntile(4).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Ntile_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT NTILE(4) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Ntile(4).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
