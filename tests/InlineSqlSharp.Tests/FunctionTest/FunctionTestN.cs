using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void NVL_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(NVL(_t.name, L("Unknown")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NVL(\"t\".name, 'Unknown')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void NVL_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(NVL(_t.created_at, TO_DATE(L("2000/01/01"), L("YYYY/MM/DD"))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NVL(\"t\".created_at, TO_DATE('2000/01/01', 'YYYY/MM/DD'))");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void NVL_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(NVL(_t.code, L(0)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NVL(\"t\".code, 0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
