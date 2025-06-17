using System.Text;
using static SqlArtisan.Sql;

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
    public void Substrb_CharacterPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(Substrb(_t.Name, 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SUBSTRB(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Substrb_CharacterPositionLength_CorrectSql()
    {
        SqlStatement sql =
            Select(Substrb(_t.Name, 1, 3))
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
    public void Sysdate_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Systimestamp_NoParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(Systimestamp)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSTIMESTAMP");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
