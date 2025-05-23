using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void NextVal_SequenceName_CorrectSql()
    {
        SqlStatement sql =
            Select(NextVal("seq_test"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXTVAL('seq_test')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

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
