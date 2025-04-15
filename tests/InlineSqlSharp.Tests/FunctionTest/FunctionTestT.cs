using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void TO_DATE_CharacterValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_DATE("2001/02/03", "YYYY/MM/DD"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_DATE(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRIM_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRIM(_t.name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRIM_CharacterValueWithTrimChar_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRIM(_t.name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRIM(BOTH :0 FROM \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_CHAR_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_CHAR(_t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_CHAR_DateTimValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_CHAR(_t.created_at, "YYYY-MM-DD"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".created_at, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_CHAR_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_CHAR(_t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_CHAR_NumericValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_CHAR(_t.code, "999"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_CHAR(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_DATE_CharacterValueWithStringFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_DATE(L("2001/02/03"), "YYYY/MM/DD"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_DATE('2001/02/03', 'YYYY/MM/DD')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_NUMBER_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_NUMBER("01"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_NUMBER(:0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TO_NUMBER_CharacterValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TO_NUMBER("100.00", "9G999D99"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TO_NUMBER(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRUNC_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRUNC(_t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRUNC_DateTimeValueWithFormat_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRUNC(_t.created_at, "MONTH"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".created_at, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRUNC_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRUNC(_t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TRUNC_NumericValueWithDecimalPlaces_CorrectSql()
    {
        SqlStatement sql =
            SELECT(TRUNC(_t.code, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TRUNC(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
