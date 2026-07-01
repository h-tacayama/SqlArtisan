using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class JsonOperatorTests
{
    private readonly TestTable _t = new("t");

    // --- JsonArrow (->) ---------------------------------------------------------

    [Fact]
    public void JsonArrow_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "key"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonArrow_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "key"))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT (`t`.name -> ?0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void JsonArrow_Sqlite_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "key"))
            .Build(Dbms.Sqlite);

        Assert.Equal("SELECT (\"t\".name -> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonArrow_Nested_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(JsonArrow(_t.Name, "a"), "b"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ((\"t\".name -> :0) -> :1)", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    // --- JsonArrowText (->>) ----------------------------------------------------

    [Fact]
    public void JsonArrowText_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, "key"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonArrowText_InWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonArrowText(_t.Name, "status") == "active")
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".name ->> :0) = :1",
            sql.Text);
        Assert.Equal("status", sql.Parameters.Get<string>(":0"));
        Assert.Equal("active", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void JsonArrowText_WithAlias_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, "city").As("city"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> :0) \"city\"", sql.Text);
    }

    // --- JsonHashArrow (#>) -----------------------------------------------------

    [Fact]
    public void JsonHashArrow_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonHashArrow(_t.Name, "{a,b}"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name #> :0)", sql.Text);
        Assert.Equal("{a,b}", sql.Parameters.Get<string>(":0"));
    }

    // --- JsonHashArrowText (#>>) ------------------------------------------------

    [Fact]
    public void JsonHashArrowText_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonHashArrowText(_t.Name, "{a,b}"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name #>> :0)", sql.Text);
        Assert.Equal("{a,b}", sql.Parameters.Get<string>(":0"));
    }
}
