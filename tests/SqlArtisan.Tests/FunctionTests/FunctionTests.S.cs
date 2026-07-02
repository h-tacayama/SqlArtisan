using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Score_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(Score(1))
            .From(_t)
            .Where(ContainsScore(_t.Name, "database", 1) > 0)
            .OrderBy(Score(1).Desc)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT SCORE(1) ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE CONTAINS(\"t\".name, :0, 1) > :1 ");
        expected.Append("ORDER BY SCORE(1) DESC");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>(":0"));
        Assert.Equal(0, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Sign_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Sign(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SIGN(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Sqrt_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Sqrt(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SQRT(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
    public void Sum_DistinctNumericValue_CorrectSql()
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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSTIMESTAMP");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
