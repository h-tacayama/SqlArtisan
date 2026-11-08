using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Floor_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Floor(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("FLOOR(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Format_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Format(_t.CreatedAt, "yyyy-MM"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("FORMAT(\"t\".created_at, @0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("yyyy-MM", sql.Parameters.Get<string>("@0"));
    }

    [Fact]
    public void Format_SqlServer_WithCulture_CorrectSql()
    {
        SqlStatement sql =
            Select(Format(_t.CreatedAt, "yyyy-MM", "en-US"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("FORMAT(\"t\".created_at, @0, @1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("yyyy-MM", sql.Parameters.Get<string>("@0"));
        Assert.Equal("en-US", sql.Parameters.Get<string>("@1"));
    }

    [Fact]
    public void Freetext_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Freetext(_t.Name, "how to build a query"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE FREETEXT(\"t\".name, @0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("how to build a query", sql.Parameters.Get<string>("@0"));
    }

    [Fact]
    public void Function_ColumnAndValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Function("my_func", _t.Code, 10))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT my_func(\"t\".code, :0) ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(10, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Function_NoArguments_CorrectSql()
    {
        SqlStatement sql =
            Select(Function("now"))
            .Build();

        Assert.Equal("SELECT now()", sql.Text);
    }

    [Fact]
    public void Function_Oracle_DottedName_CorrectSql()
    {
        SqlStatement sql =
            Select(Function("app.my_pkg.Format$Code", _t.Name))
            .From(_t)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT app.my_pkg.Format$Code(\"t\".name) ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Function_SqlServer_ExpressionNullAndSubquery_CorrectSql()
    {
        SqlStatement sql =
            Select(Function("dbo._f", Upper(_t.Name), Null, Select(Max(_t.Code)).From(_t)))
            .From(_t)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT dbo._f(UPPER(\"t\".name), NULL, ");
        expected.Append("(SELECT MAX(\"t\".code) FROM test_table \"t\")) ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Function_BlankName_ThrowsArgumentException(string? name)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Function(name!, _t.Code));

        Assert.Equal("A function call requires a function name.", ex.Message);
    }

    [Theory]
    [InlineData("1f")]
    [InlineData("f; DROP TABLE users; --")]
    [InlineData("f(1)")]
    [InlineData("f#")]
    [InlineData("f x")]
    [InlineData("f-x")]
    [InlineData("f/**/")]
    [InlineData(".f")]
    [InlineData("f.")]
    [InlineData("a..f")]
    [InlineData("$f")]
    [InlineData("\"F\"")]
    [InlineData("[dbo].[f]")]
    [InlineData("f\u00e9")]
    [InlineData("f\u02bc")]
    public void Function_NameNotIdentifierShaped_ThrowsArgumentException(string name)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Function(name, _t.Code));

        Assert.Equal(
            "A function name must be ASCII identifiers joined by '.', each starting "
            + "with a letter or _ and holding only letters, digits, _ and $.",
            ex.Message);
    }

    [Fact]
    public void Function_NullArguments_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => Function("f", null!));

        Assert.Equal(
            "A C# null is not SQL NULL; pass Sql.Null instead. (Parameter 'args')",
            ex.Message);
    }
}
