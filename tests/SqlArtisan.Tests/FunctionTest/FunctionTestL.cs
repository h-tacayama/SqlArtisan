using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void LastDay_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(LastDay(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LAST_DAY(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Least_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Least(_t.Code, 10, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LEAST(\"t\".code, :0, \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Length_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Length(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LENGTH(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LengthB_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(LengthB(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LENGTHB(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Lower_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Lower(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LOWER(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LPad_CharacterAndLength_CorrectSql()
    {
        SqlStatement sql =
            Select(LPad(_t.Name, 10))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LPAD(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LPad_CharacterLengthAndPadding_CorrectSql()
    {
        SqlStatement sql =
            Select(LPad(_t.Name, 10, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LPAD(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LTRIM_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(LTrim(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LTRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LTrim_CharacterAndTrimChars_CorrectSql()
    {
        SqlStatement sql =
            Select(LTrim(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("LTRIM(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
