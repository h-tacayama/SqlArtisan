using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class JsonOperatorTests
{
    private readonly TestTable _t = new("t");

    // --- JsonGet (->) ---------------------------------------------------------

    [Fact]
    public void JsonGet_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGet(_t.Name, "key"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonGet_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGet(_t.Name, "key"))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT (`t`.name -> ?0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void JsonGet_Sqlite_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGet(_t.Name, "key"))
            .Build(Dbms.Sqlite);

        Assert.Equal("SELECT (\"t\".name -> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonGet_Nested_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGet(JsonGet(_t.Name, "a"), "b"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ((\"t\".name -> :0) -> :1)", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    // --- JsonGetText (->>) ----------------------------------------------------

    [Fact]
    public void JsonGetText_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGetText(_t.Name, "key"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonGetText_InWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonGetText(_t.Name, "status") == "active")
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".name ->> :0) = :1",
            sql.Text);
        Assert.Equal("status", sql.Parameters.Get<string>(":0"));
        Assert.Equal("active", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void JsonGetText_WithAlias_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGetText(_t.Name, "city").As("city"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> :0) \"city\"", sql.Text);
    }

    // --- JsonGetPath (#>) -----------------------------------------------------

    [Fact]
    public void JsonGetPath_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGetPath(_t.Name, "{a,b}"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name #> :0)", sql.Text);
        Assert.Equal("{a,b}", sql.Parameters.Get<string>(":0"));
    }

    // --- JsonGetPathText (#>>) ------------------------------------------------

    [Fact]
    public void JsonGetPathText_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonGetPathText(_t.Name, "{a,b}"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name #>> :0)", sql.Text);
        Assert.Equal("{a,b}", sql.Parameters.Get<string>(":0"));
    }
}
