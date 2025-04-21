using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void REGEXP_COUNT_Pattern_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_COUNT(_t.name, "[abc]"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_COUNT_PatternPosition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_COUNT(_t.name, "[abc]", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_COUNT_PatternPositionOptions_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_COUNT(_t.name, "[abc]", 2, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0, :1, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_REPLACE_PatternReplacement_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_REPLACE(_t.name, "[abc]", "x"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_REPLACE_PatternReplacementPosition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_REPLACE(_t.name, "[abc]", "x", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_REPLACE_PatternReplacementPositionOccurrence_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_REPLACE(_t.name, "[abc]", "x", 2, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2, :3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_REPLACE_PatternReplacementPositionOccurrenceOptions_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_REPLACE(_t.name, "[abc]", "x", 2, 3, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2, :3, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_SUBSTR_Pattern_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_SUBSTR(_t.name, "[abc]"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_SUBSTR_PatternPosition_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_SUBSTR(_t.name, "[abc]", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_SUBSTR_PatternPositionOccurrence_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_SUBSTR(_t.name, "[abc]", 2, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_SUBSTR_PatternPositionOccurrenceOptions_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_SUBSTR(_t.name, "[abc]", 2, 3, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REGEXP_SUBSTR_PatternPositionOccurrenceOptionsSubPattern_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REGEXP_SUBSTR(_t.name, "[abc]", 2, 3, RegexpOptions.None, 4))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2, '', :3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void REPLACE_CharacterSearchAndReplacement_CorrectSql()
    {
        SqlStatement sql =
            SELECT(REPLACE(_t.name, "a", "b"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REPLACE(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RPAD_CharacterLength_CorrectSql()
    {
        SqlStatement sql =
            SELECT(RPAD(_t.name, 10))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RPAD_CharacterLengthPadding_CorrectSql()
    {
        SqlStatement sql =
            SELECT(RPAD(_t.name, 10, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RTRIM_Character_CorrectSql()
    {
        SqlStatement sql =
            SELECT(RTRIM(_t.name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RTRIM_CharacterTrimChars_CorrectSql()
    {
        SqlStatement sql =
            SELECT(RTRIM(_t.name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
