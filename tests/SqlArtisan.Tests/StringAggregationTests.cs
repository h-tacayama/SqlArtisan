using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class StringAggregationTests
{
    private readonly TestTable _t = new();

    // --- STRING_AGG (PostgreSQL / SQL Server) ---------------------------------

    [Fact]
    public void StringAgg_Plain_CorrectSql()
    {
        // Arrange
        string expected = "SELECT STRING_AGG(name, :0)";

        // Act
        SqlStatement sql =
            Select(StringAgg(_t.Name, ", ")).Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(", ", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void StringAgg_InlineOrderBy_PostgreSql_CorrectSql()
    {
        // Arrange
        string expected = "SELECT STRING_AGG(name, :0 ORDER BY name)";

        // Act
        SqlStatement sql =
            Select(StringAgg(_t.Name, ", ").OrderBy(_t.Name)).Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void StringAgg_WithinGroup_SqlServer_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT STRING_AGG(name, @0) WITHIN GROUP (ORDER BY name DESC)";

        // Act
        SqlStatement sql =
            Select(StringAgg(_t.Name, ", ").WithinGroup(OrderBy(_t.Name.Desc)))
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    // --- LISTAGG (Oracle) -----------------------------------------------------

    [Fact]
    public void Listagg_WithinGroup_Oracle_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT LISTAGG(name, :0) WITHIN GROUP (ORDER BY name)";

        // Act
        SqlStatement sql =
            Select(Listagg(_t.Name, ", ").WithinGroup(OrderBy(_t.Name)))
            .Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(", ", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Listagg_Plain_CorrectSql()
    {
        // Arrange
        string expected = "SELECT LISTAGG(name, :0)";

        // Act
        SqlStatement sql = Select(Listagg(_t.Name, ", ")).Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    // --- GROUP_CONCAT (MySQL / SQLite) ----------------------------------------

    [Fact]
    public void GroupConcat_Plain_CorrectSql()
    {
        // Arrange
        string expected = "SELECT GROUP_CONCAT(name)";

        // Act
        SqlStatement sql = Select(GroupConcat(_t.Name)).Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void GroupConcat_PositionalSeparator_Sqlite_CorrectSql()
    {
        // Arrange
        string expected = "SELECT GROUP_CONCAT(name, :0)";

        // Act
        SqlStatement sql =
            Select(GroupConcat(_t.Name, ", ")).Build(Dbms.Sqlite);

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(", ", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void GroupConcat_SeparatorKeyword_MySql_CorrectSql()
    {
        // Arrange
        string expected = "SELECT GROUP_CONCAT(name SEPARATOR ', ')";

        // Act
        SqlStatement sql =
            Select(GroupConcat(_t.Name, Separator(", "))).Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void GroupConcat_SeparatorWithQuote_MySql_EscapesLiteral()
    {
        // Arrange
        string expected = @"SELECT GROUP_CONCAT(name SEPARATOR ''' \\')";

        // Act
        SqlStatement sql =
            Select(GroupConcat(_t.Name, Separator(@"' \"))).Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void GroupConcat_OrderBySeparator_MySql_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT GROUP_CONCAT(name ORDER BY name DESC SEPARATOR ', ')";

        // Act
        SqlStatement sql =
            Select(GroupConcat(_t.Name, Separator(", ")).OrderBy(_t.Name.Desc))
            .Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void GroupConcat_Distinct_MySql_CorrectSql()
    {
        // Arrange
        string expected = "SELECT GROUP_CONCAT(DISTINCT name)";

        // Act
        SqlStatement sql =
            Select(GroupConcat(Distinct, _t.Name)).Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void GroupConcat_DistinctSeparator_MySql_CorrectSql()
    {
        // Arrange
        string expected = "SELECT GROUP_CONCAT(DISTINCT name SEPARATOR ' | ')";

        // Act
        SqlStatement sql =
            Select(GroupConcat(Distinct, _t.Name, Separator(" | ")))
            .Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected, sql.Text);
    }
}
