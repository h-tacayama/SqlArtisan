using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class UsingTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void Using_SingleColumn_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .InnerJoin(_s)
            .Using(_t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("INNER JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("USING ");
        expected.Append("(code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Using_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .LeftJoin(_s)
            .Using(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("LEFT JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("USING ");
        expected.Append("(code, name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
