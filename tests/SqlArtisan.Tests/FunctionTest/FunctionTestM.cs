using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Max_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Max(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MAX(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Min_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Min(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MIN(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Mod_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Mod(_t.Code, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MOD(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MonthsBetween_DateTimeValues_CorrectSql()
    {
        SqlStatement sql =
            Select(
                MonthsBetween(
                    ToDate("2001/02/03", "YYYY/MM/DD"),
                    ToDate("2004/05/06", "YYYY/MM/DD")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MONTHS_BETWEEN(TO_DATE(:0, :1), ");
        expected.Append("TO_DATE(:2, :3))");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
