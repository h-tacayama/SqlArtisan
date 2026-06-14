using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Floor_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Floor(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("FLOOR(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
