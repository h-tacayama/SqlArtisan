using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class NaturalLeftJoinTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void NaturalLeftJoin_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .NaturalLeftJoin(_s)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("NATURAL LEFT JOIN ");
        expected.Append("test_table \"s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
