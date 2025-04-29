using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class CrossJoinTest
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void CrossJoin_SimpleCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .CrossJoin(_s)
            .Where(_t.Code == _s.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
