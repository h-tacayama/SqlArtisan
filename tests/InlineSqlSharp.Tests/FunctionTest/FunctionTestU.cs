using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void UPPER_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(UPPER(_t.name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("UPPER(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
