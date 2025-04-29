using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void RegexpCount_Pattern_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpCount(_t.Name, "[abc]"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpCount_PatternPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpCount(_t.Name, "[abc]", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpCount_PatternPositionOptions_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpCount(_t.Name, "[abc]", 2, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_COUNT(\"t\".name, :0, :1, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpReplace_PatternReplacement_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpReplace(_t.Name, "[abc]", "x"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpReplace_PatternReplacementPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpReplace(_t.Name, "[abc]", "x", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpReplace_PatternReplacementPositionOccurrence_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpReplace(_t.Name, "[abc]", "x", 2, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2, :3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpReplace_PatternReplacementPositionOccurrenceOptions_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpReplace(_t.Name, "[abc]", "x", 2, 3, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_REPLACE(\"t\".name, :0, :1, :2, :3, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpSubstr_Pattern_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpSubstr(_t.Name, "[abc]"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpSubstr_PatternPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpSubstr(_t.Name, "[abc]", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpSubstr_PatternPositionOccurrence_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpSubstr(_t.Name, "[abc]", 2, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpSubstr_PatternPositionOccurrenceOptions_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpSubstr(_t.Name, "[abc]", 2, 3, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RegexpSubstr_PatternPositionOccurrenceOptionsSubPattern_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpSubstr(_t.Name, "[abc]", 2, 3, RegexpOptions.None, 4))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_SUBSTR(\"t\".name, :0, :1, :2, '', :3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Replace_CharacterSearchAndReplacement_CorrectSql()
    {
        SqlStatement sql =
            Select(Replace(_t.Name, "a", "b"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REPLACE(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RPad_CharacterLength_CorrectSql()
    {
        SqlStatement sql =
            Select(RPad(_t.Name, 10))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RPad_CharacterLengthPadding_CorrectSql()
    {
        SqlStatement sql =
            Select(RPad(_t.Name, 10, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RTrim_Character_CorrectSql()
    {
        SqlStatement sql =
            Select(RTrim(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void RTrim_CharacterTrimChars_CorrectSql()
    {
        SqlStatement sql =
            Select(RTrim(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
