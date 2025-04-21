using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void MAX_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(MAX(_t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MAX(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MIN_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(MIN(_t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MIN(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MOD_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            SELECT(MOD(_t.code, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MOD(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MONTHS_BETWEEN_DateTimeValues_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                MONTHS_BETWEEN(
                    TO_DATE("2001/02/03", "YYYY/MM/DD"),
                    TO_DATE("2004/05/06", "YYYY/MM/DD")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MONTHS_BETWEEN(TO_DATE(:0, :1), ");
        expected.Append("TO_DATE(:2, :3))");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
