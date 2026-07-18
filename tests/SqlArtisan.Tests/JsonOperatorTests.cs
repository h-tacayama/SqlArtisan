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

    // --- JsonbContains (@>) -----------------------------------------------------

    [Fact]
    public void JsonbContains_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbContains(_t.Name, "{\"a\":1}"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name @> :0",
            sql.Text);
        Assert.Equal("{\"a\":1}", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonbContains_CastJsonbValue_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbContains(_t.Name, Cast("{\"a\":1}", "jsonb")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name @> CAST(:0 AS jsonb)",
            sql.Text);
        Assert.Equal("{\"a\":1}", sql.Parameters.Get<string>(":0"));
    }

    // --- JsonbExists (?) --------------------------------------------------------

    [Fact]
    public void JsonbExists_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExists(_t.Name, "city"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name ? :0",
            sql.Text);
        Assert.Equal("city", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonbExists_WithAndCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExists(_t.Name, "city") & (_t.Name == "x"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".name ? :0) AND (\"t\".name = :1)",
            sql.Text);
        Assert.Equal("city", sql.Parameters.Get<string>(":0"));
        Assert.Equal("x", sql.Parameters.Get<string>(":1"));
    }

    // --- JsonbExistsAll (?&) ----------------------------------------------------

    [Fact]
    public void JsonbExistsAll_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExistsAll(_t.Name, "city", "zip"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name ?& ARRAY[:0, :1]",
            sql.Text);
        Assert.Equal("city", sql.Parameters.Get<string>(":0"));
        Assert.Equal("zip", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void JsonbExistsAll_NoKeys_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            JsonbExistsAll(_t.Name));

        Assert.Equal("?& requires at least one key.", ex.Message);
    }

    // --- JsonbExistsAny (?|) ----------------------------------------------------

    [Fact]
    public void JsonbExistsAny_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExistsAny(_t.Name, "city", "zip"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name ?| ARRAY[:0, :1]",
            sql.Text);
        Assert.Equal("city", sql.Parameters.Get<string>(":0"));
        Assert.Equal("zip", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void JsonbExistsAny_SingleKey_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExistsAny(_t.Name, "city"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name ?| ARRAY[:0]",
            sql.Text);
        Assert.Equal("city", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonbExistsAny_NoKeys_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            JsonbExistsAny(_t.Name));

        Assert.Equal("?| requires at least one key.", ex.Message);
    }
}
