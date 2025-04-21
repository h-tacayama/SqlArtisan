using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DeleteTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void DELETE_FROM_SimpleTable_CorrectSql()
    {
        SqlStatement sql =
            DELETE_FROM(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DELETE_FROM_WithWhereClause_CorrectSql()
    {
        SqlStatement sql =
            DELETE_FROM(_t)
            .WHERE(_t.code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
