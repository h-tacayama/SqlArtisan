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
        Assert.Equal("123", sql.Parameters.Get<string>(":0"));
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
        Assert.Equal("5", sql.Parameters.Get<string>(":0"));
    }

    // The target type is an identifier position — a type name cannot be quoted
    // at all (`DECIMAL(10,2)`), so it is emitted verbatim (ADR 0016).
    [Fact]
    public void Cast_TypeWithQuote_EmitsVerbatim()
    {
        // Arrange
        string expected = "SELECT CAST(code AS INT) ; DROP TABLE users --) FROM test_table";

        // Act
        SqlStatement sql =
            Select(Cast(_t.Code, "INT) ; DROP TABLE users --"))
            .From(_t)
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Cast_NullType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Cast(_t.Code, null!));

        Assert.Equal("CAST requires a target type.", ex.Message);
    }

    [Fact]
    public void Cast_EmptyType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Cast(_t.Code, ""));

        Assert.Equal("CAST requires a target type.", ex.Message);
    }

    [Fact]
    public void Cast_WhiteSpaceType_ThrowsArgumentException()
    {
        // The type is emitted as a bare token, so whitespace would render
        // `CAST(x AS  )` — invalid on every dialect.
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Cast(_t.Code, "  "));

        Assert.Equal("CAST requires a target type.", ex.Message);
    }
}
