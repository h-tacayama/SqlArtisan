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

        Assert.Equal("SELECT (\"t\".name -> 'key')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonArrow_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "$.key"))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT (`t`.name -> '$.key')", sql.Text);
    }

    [Fact]
    public void JsonArrow_Sqlite_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "$.key"))
            .Build(Dbms.Sqlite);

        Assert.Equal("SELECT (\"t\".name -> '$.key')", sql.Text);
    }

    [Fact]
    public void JsonArrow_KeyWithQuote_EscapesLiteral()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "it's"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> 'it''s')", sql.Text);
    }

    [Fact]
    public void JsonArrow_MySqlKeyWithBackslash_EscapesLiteral()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, "$.a\\b"))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT (`t`.name -> '$.a\\\\b')", sql.Text);
    }

    [Fact]
    public void JsonArrow_NullKey_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => JsonArrow(_t.Name, (string)null!));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void JsonArrow_Index_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, 0), JsonArrow(_t.Name, -1))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> 0), (\"t\".name -> -1)", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonArrow_CharKey_EmitsStringLiteral()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, 'a'), JsonArrowText(_t.Name, 'b'))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> 'a'), (\"t\".name ->> 'b')", sql.Text);
    }

    [Fact]
    public void JsonArrow_ExpressionKey_EmitsExpression()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, _t.Code))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> \"t\".code)", sql.Text);
    }

    [Fact]
    public void JsonArrow_BindValueKey_Binds()
    {
        SqlStatement sql =
            Select(JsonArrow(_t.Name, Bind("key")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name -> :0)", sql.Text);
        Assert.Equal("key", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonArrow_Nested_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrow(JsonArrow(_t.Name, "a"), "b"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ((\"t\".name -> 'a') -> 'b')", sql.Text);
    }

    // --- JsonArrowText (->>) ----------------------------------------------------

    [Fact]
    public void JsonArrowText_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, "key"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> 'key')", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void JsonArrowText_Index_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, 2))
            .Build(Dbms.Sqlite);

        Assert.Equal("SELECT (\"t\".name ->> 2)", sql.Text);
    }

    [Fact]
    public void JsonArrowText_BindValueKey_Binds()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, Bind("key")))
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
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".name ->> 'status') = :0",
            sql.Text);
        Assert.Equal("active", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void JsonArrowText_WithAlias_CorrectSql()
    {
        SqlStatement sql =
            Select(JsonArrowText(_t.Name, "city").As("city"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (\"t\".name ->> 'city') \"city\"", sql.Text);
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

    [Fact]
    public void JsonbContains_NullValue_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => JsonbContains(_t.Name, null!));

        Assert.Equal(
            "@> cannot compare a C# null; test for NULL with .IsNull or .IsNotNull. "
                + "(Parameter 'jsonValue')",
            ex.Message);
    }

    [Fact]
    public void JsonbExists_NullKey_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => JsonbExists(_t.Name, null!));

        Assert.Equal(
            "? cannot compare a C# null; test for NULL with .IsNull or .IsNotNull. "
                + "(Parameter 'key')",
            ex.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonbExistsAllOrAny_NullJsonExpr_ThrowsArgumentNullException(bool all)
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => all ? JsonbExistsAll(null!, "a") : JsonbExistsAny(null!, "a"));

        Assert.Equal(
            $"{(all ? "?&" : "?|")} cannot compare a C# null; "
                + "test for NULL with .IsNull or .IsNotNull. (Parameter 'jsonExpr')",
            ex.Message);
    }
}
