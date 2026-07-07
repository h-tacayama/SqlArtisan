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
}
