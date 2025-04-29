using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Substr_CharacterPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(Substr(_t.Name, 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Substr_CharacterPositionLength_CorrectSql()
    {
        SqlStatement sql =
            Select(Substr(_t.Name, 1, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SubstrB_CharacterPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(SubstrB(_t.Name, 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTRB(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SubstrB_CharacterPositionLength_CorrectSql()
    {
        SqlStatement sql =
            Select(SubstrB(_t.Name, 1, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTRB(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Sum_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Sum(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUM(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Sum_Distinct_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Sum(Distinct, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUM(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SysDate_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(SysDate)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SysTimestamp_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(SysTimestamp)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSTIMESTAMP");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
