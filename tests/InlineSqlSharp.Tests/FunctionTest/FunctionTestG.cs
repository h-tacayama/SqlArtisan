using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void GREATEST_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            SELECT(GREATEST(_t.code, 10, _t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GREATEST(\"t\".code, :0, \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
