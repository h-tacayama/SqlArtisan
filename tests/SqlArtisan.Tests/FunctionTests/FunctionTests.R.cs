using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
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
    public void RegexpInstr_Pattern_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void RegexpInstr_PatternPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]", 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void RegexpInstr_PatternPositionOccurrence_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]", 2, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
        Assert.Equal(3, sql.Parameters.Get<int>(":2"));
    }

    [Fact]
    public void RegexpInstr_PatternPositionOccurrenceReturnOption_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]", 2, 3, 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0, :1, :2, :3)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
        Assert.Equal(3, sql.Parameters.Get<int>(":2"));
        Assert.Equal(1, sql.Parameters.Get<int>(":3"));
    }

    [Fact]
    public void RegexpInstr_PatternPositionOccurrenceReturnOptionOptions_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]", 2, 3, 1, RegexpOptions.CaseInsensitive))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0, :1, :2, :3, 'i')");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
        Assert.Equal(3, sql.Parameters.Get<int>(":2"));
        Assert.Equal(1, sql.Parameters.Get<int>(":3"));
    }

    [Fact]
    public void RegexpInstr_PatternPositionOccurrenceReturnOptionOptionsSubPattern_CorrectSql()
    {
        SqlStatement sql =
            Select(RegexpInstr(_t.Name, "[abc]", 2, 3, 1, RegexpOptions.None, 4))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("REGEXP_INSTR(\"t\".name, :0, :1, :2, :3, '', :4)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("[abc]", sql.Parameters.Get<string>(":0"));
        Assert.Equal(2, sql.Parameters.Get<int>(":1"));
        Assert.Equal(3, sql.Parameters.Get<int>(":2"));
        Assert.Equal(1, sql.Parameters.Get<int>(":3"));
        Assert.Equal(4, sql.Parameters.Get<int>(":4"));
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
    public void Right_CharacterAndLength_CorrectSql()
    {
        SqlStatement sql =
            Select(Right(_t.Name, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RIGHT(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Round_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Round(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ROUND(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Round_NumericValueDecimals_CorrectSql()
    {
        SqlStatement sql =
            Select(Round(_t.Code, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ROUND(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rpad_CharacterLength_CorrectSql()
    {
        SqlStatement sql =
            Select(Rpad(_t.Name, 10))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rpad_CharacterLengthPadding_CorrectSql()
    {
        SqlStatement sql =
            Select(Rpad(_t.Name, 10, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RPAD(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rtrim_Character_CorrectSql()
    {
        SqlStatement sql =
            Select(Rtrim(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rtrim_CharacterTrimChars_CorrectSql()
    {
        SqlStatement sql =
            Select(Rtrim(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("RTRIM(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
