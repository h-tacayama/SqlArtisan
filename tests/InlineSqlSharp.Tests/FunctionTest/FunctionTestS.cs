using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void SUBSTR_CharacterPosition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUBSTR(_t.name, L(1)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTR(\"t\".name, 1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SUBSTR_CharacterPositionLength_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUBSTR(_t.name, L(1), L(3)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTR(\"t\".name, 1, 3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SUBSTRB_CharacterPosition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUBSTRB(_t.name, L(1)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTRB(\"t\".name, 1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SUBSTRB_CharacterPositionLength_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUBSTRB(_t.name, L(1), L(3)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTRB(\"t\".name, 1, 3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SUM_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUM(_t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUM(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SUM_DISTINCT_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SUM(DISTINCT, _t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUM(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SYSDATE_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SYSDATE)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SYSTIMESTAMP_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SYSTIMESTAMP)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSTIMESTAMP");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
