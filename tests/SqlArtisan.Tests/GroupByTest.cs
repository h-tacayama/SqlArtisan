using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class GroupByTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void GroupBy_SingleColumn_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupBy_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(
                _t.Code,
                _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
