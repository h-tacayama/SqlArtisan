using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Upper_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Upper(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("UPPER(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
