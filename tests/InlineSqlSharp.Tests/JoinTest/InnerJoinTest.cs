using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InnerJoinTest
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void InnerJoin_SimpleCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .InnerJoin(_s)
            .On(_t.Code == _s.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("INNER JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InnerJoin_ComplexConditionWithWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .InnerJoin(_s)
            .On(
                And(
                    _t.Code == _s.Code,
                    _t.Name == _s.Name))
            .Where(_t.Code > 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("INNER JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("ON ");
        expected.Append("(");
        expected.Append("\"t\".code = \"s\".code");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".name = \"s\".name");
        expected.Append(") ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
