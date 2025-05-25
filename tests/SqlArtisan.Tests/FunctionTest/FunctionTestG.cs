using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Greatest_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Greatest(_t.Code, 10, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GREATEST(\"t\".code, :0, \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
