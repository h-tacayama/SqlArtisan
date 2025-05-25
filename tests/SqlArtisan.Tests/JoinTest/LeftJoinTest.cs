using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class LeftJoinTest
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void LeftJoin_SimpleCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .LeftJoin(_s)
            .On(_t.Code == _s.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("LEFT JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
