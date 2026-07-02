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
