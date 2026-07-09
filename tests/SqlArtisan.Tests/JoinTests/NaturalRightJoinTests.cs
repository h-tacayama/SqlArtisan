using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class NaturalRightJoinTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void NaturalRightJoin_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .NaturalRightJoin(_s)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("NATURAL RIGHT JOIN ");
        expected.Append("test_table \"s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
