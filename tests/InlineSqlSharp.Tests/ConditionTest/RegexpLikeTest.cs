using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class RegexpLikeTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public RegexpLikeTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void REGEXP_LIKE_NoOptions_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]"),
            "REGEXP_LIKE(\"t\".name, :0)",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_CaseSensitive_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]", RegexpOptions.CaseSensitive),
            "REGEXP_LIKE(\"t\".name, :0, 'c')",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_CaseInsensitive_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]", RegexpOptions.CaseInsensitive),
            "REGEXP_LIKE(\"t\".name, :0, 'i')",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_MultipleLines_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]", RegexpOptions.MultipleLines),
            "REGEXP_LIKE(\"t\".name, :0, 'm')",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_NewLine_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]", RegexpOptions.NewLine),
            "REGEXP_LIKE(\"t\".name, :0, 'n')",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_ExcludingWhiteSpace_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]", RegexpOptions.ExcludingWhiteSpace),
            "REGEXP_LIKE(\"t\".name, :0, 'x')",
            1, "[2-5]");

    [Fact]
    public void REGEXP_LIKE_AllOptions_CorrectSql() =>
        _assert.Equal(
            REGEXP_LIKE(_t.name, "[2-5]",
                RegexpOptions.CaseSensitive
                | RegexpOptions.CaseInsensitive
                | RegexpOptions.MultipleLines
                | RegexpOptions.NewLine
                | RegexpOptions.ExcludingWhiteSpace),
            "REGEXP_LIKE(\"t\".name, :0, 'cimnx')",
            1, "[2-5]");
}
