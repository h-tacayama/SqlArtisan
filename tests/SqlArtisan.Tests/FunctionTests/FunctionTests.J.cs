using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void JsonExtract_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonExtract(_t.Name, "$.address"))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT JSON_EXTRACT(`t`.name, '$.address')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonExtract_Sqlite_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonExtract(_t.Name, "$.address"))
            .Build(Dbms.Sqlite);

        Assert.Equal("SELECT JSON_EXTRACT(\"t\".name, '$.address')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonExtract_NestedPath_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonExtract(_t.Name, "$.store.book[0].title"))
            .Build(Dbms.MySql);

        Assert.Equal(
            "SELECT JSON_EXTRACT(`t`.name, '$.store.book[0].title')",
            sql.Text);
    }

    [Fact]
    public void JsonExtract_InWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonExtract(_t.Name, "$.age") > 18)
            .Build(Dbms.MySql);

        Assert.Equal(
            "SELECT `t`.name FROM test_table `t` WHERE JSON_EXTRACT(`t`.name, '$.age') > ?0",
            sql.Text);
        Assert.Equal(18, sql.Parameters.Get<int>("?0"));
    }

    [Fact]
    public void JsonQuery_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonQuery(_t.Name, "$.address"))
            .Build(Dbms.SqlServer);

        Assert.Equal("SELECT JSON_QUERY(\"t\".name, '$.address')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonQuery_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonQuery(_t.Name, "$.items"))
            .Build(Dbms.Oracle);

        Assert.Equal("SELECT JSON_QUERY(\"t\".name, '$.items')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonValue_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonValue(_t.Name, "$.name"))
            .Build(Dbms.SqlServer);

        Assert.Equal("SELECT JSON_VALUE(\"t\".name, '$.name')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonValue_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonValue(_t.Name, "$.name"))
            .Build(Dbms.Oracle);

        Assert.Equal("SELECT JSON_VALUE(\"t\".name, '$.name')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonValue_SqlServer_InWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonValue(_t.Name, "$.status") == "active")
            .Build(Dbms.SqlServer);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE JSON_VALUE(\"t\".name, '$.status') = @0",
            sql.Text);
        Assert.Equal("active", sql.Parameters.Get<string>("@0"));
    }

    [Fact]
    public void JsonExtract_NullPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonExtract(_t.Name, null!));

        Assert.Equal("JSON_EXTRACT requires a path.", ex.Message);
    }

    [Fact]
    public void JsonExtract_EmptyPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonExtract(_t.Name, ""));

        Assert.Equal("JSON_EXTRACT requires a path.", ex.Message);
    }

    [Fact]
    public void JsonQuery_NullPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonQuery(_t.Name, null!));

        Assert.Equal("JSON_QUERY requires a path.", ex.Message);
    }

    [Fact]
    public void JsonQuery_EmptyPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonQuery(_t.Name, ""));

        Assert.Equal("JSON_QUERY requires a path.", ex.Message);
    }

    [Fact]
    public void JsonValue_NullPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonValue(_t.Name, null!));

        Assert.Equal("JSON_VALUE requires a path.", ex.Message);
    }

    [Fact]
    public void JsonValue_EmptyPath_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => JsonValue(_t.Name, ""));

        Assert.Equal("JSON_VALUE requires a path.", ex.Message);
    }
}
