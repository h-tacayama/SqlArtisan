using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class CastTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Cast_Column_CorrectSql()
    {
        // Arrange
        string expected = "SELECT CAST(code AS VARCHAR(10)) FROM test_table";

        // Act
        SqlStatement sql =
            Select(Cast(_t.Code, "VARCHAR(10)"))
            .From(_t)
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Cast_Literal_CorrectSql()
    {
        // Arrange
        string expected = "SELECT CAST(:0 AS INTEGER) FROM test_table";

        // Act
        SqlStatement sql =
            Select(Cast("123", "INTEGER"))
            .From(_t)
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Cast_WithAlias_CorrectSql()
    {
        // Arrange
        string expected = "SELECT CAST(code AS VARCHAR(10)) \"code_str\" FROM test_table";

        // Act
        SqlStatement sql =
            Select(Cast(_t.Code, "VARCHAR(10)").As("code_str"))
            .From(_t)
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Cast_InWhere_CorrectSql()
    {
        // Arrange
        string expected = "SELECT name FROM test_table WHERE CAST(code AS VARCHAR(10)) = :0";

        // Act
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(Cast(_t.Code, "VARCHAR(10)") == "5")
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
