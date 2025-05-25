using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class RegexpLikeTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public RegexpLikeTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void RegexpLike_NoOptions_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]"),
            "REGEXP_LIKE(\"t\".name, :0)",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_CaseSensitive_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]", RegexpOptions.CaseSensitive),
            "REGEXP_LIKE(\"t\".name, :0, 'c')",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_CaseInsensitive_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]", RegexpOptions.CaseInsensitive),
            "REGEXP_LIKE(\"t\".name, :0, 'i')",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_MultipleLines_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]", RegexpOptions.MultipleLines),
            "REGEXP_LIKE(\"t\".name, :0, 'm')",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_NewLine_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]", RegexpOptions.NewLine),
            "REGEXP_LIKE(\"t\".name, :0, 'n')",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_ExcludingWhiteSpace_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]", RegexpOptions.ExcludingWhiteSpace),
            "REGEXP_LIKE(\"t\".name, :0, 'x')",
            1, "[2-5]");

    [Fact]
    public void RegexpLike_AllOptions_CorrectSql() =>
        _assert.Equal(
            RegexpLike(_t.Name, "[2-5]",
                RegexpOptions.CaseSensitive
                | RegexpOptions.CaseInsensitive
                | RegexpOptions.MultipleLines
                | RegexpOptions.NewLine
                | RegexpOptions.ExcludingWhiteSpace),
            "REGEXP_LIKE(\"t\".name, :0, 'cimnx')",
            1, "[2-5]");
}
