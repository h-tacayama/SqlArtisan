using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Nvl_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Nvl(_t.Name, "Unknown"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NVL(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
