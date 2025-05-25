using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void ToChar_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(ToChar(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToChar_DateTimeValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(ToChar(_t.CreatedAt, "YYYY-MM-DD"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".created_at, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToChar_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(ToChar(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToChar_NumericValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(ToChar(_t.Code, "999"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToDate_CharacterValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(ToDate("2001/02/03", "YYYY/MM/DD"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_DATE(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToNumber_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(ToNumber("01"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_NUMBER(:0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToNumber_CharacterValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(ToNumber("100.00", "9G999D99"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_NUMBER(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ToTimestamp_CharacterValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(ToTimestamp("2001/02/03 04:05:06", "YYYY-MM-DD HH24:MI:SS"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_TIMESTAMP(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trim_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Trim(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trim_CharacterValueWithTrimChar_CorrectSql()
    {
        SqlStatement sql =
            Select(Trim(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRIM(BOTH :0 FROM \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trunc_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Trunc(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trunc_DateTimeValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            Select(Trunc(_t.CreatedAt, "MONTH"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".created_at, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trunc_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Trunc(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Trunc_NumericValueWithDecimalPlaces_CorrectSql()
    {
        SqlStatement sql =
            Select(Trunc(_t.Code, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
