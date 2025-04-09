using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class HavingTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void HAVING_SingleCondition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.name)
            .FROM(_t)
            .GROUP_BY(_t.name)
            .HAVING(COUNT(_t.name) > L(1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name ");
        expected.Append("HAVING ");
        expected.Append("COUNT(\"t\".name) > 1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
