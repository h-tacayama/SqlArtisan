using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Power_BaseAndExponent_CorrectSql()
    {
        SqlStatement sql =
            Select(Power(_t.Code, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("POWER(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
