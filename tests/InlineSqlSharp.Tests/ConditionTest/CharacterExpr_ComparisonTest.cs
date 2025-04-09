using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CharacterExpr_ComparisonTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public CharacterExpr_ComparisonTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void CharacterExpr_Equal_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name == L("abc"), "\"t\".name = 'abc'");

    [Fact]
    public void CharacterExpr_Equal_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name == 'a', "\"t\".name = :0", 1, "a");

    [Fact]
    public void CharacterExpr_Equal_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name == "abc", "\"t\".name = :0", 1, "abc");

    [Fact]
    public void CharacterExpr_NotEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name != L("abc"), "\"t\".name <> 'abc'");

    [Fact]
    public void CharacterExpr_NotEqual_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name != 'a', "\"t\".name <> :0", 1, "a");
    [Fact]
    public void CharacterExpr_NotEqual_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name != "abc", "\"t\".name <> :0", 1, "abc");

    [Fact]
    public void CharacterExpr_LessThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name < L("abc"), "\"t\".name < 'abc'");

    [Fact]
    public void CharacterExpr_LessThan_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name < 'a', "\"t\".name < :0", 1, "a");

    [Fact]
    public void CharacterExpr_LessThan_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name < "abc", "\"t\".name < :0", 1, "abc");

    [Fact]
    public void CharacterExpr_GreaterThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name > L("abc"), "\"t\".name > 'abc'");

    [Fact]
    public void CharacterExpr_GreaterThan_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name > 'a', "\"t\".name > :0", 1, "a");

    [Fact]
    public void CharacterExpr_GreaterThan_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name > "abc", "\"t\".name > :0", 1, "abc");

    [Fact]
    public void CharacterExpr_LessEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name <= L("abc"), "\"t\".name <= 'abc'");

    [Fact]
    public void CharacterExpr_LessEqual_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name <= 'a', "\"t\".name <= :0", 1, "a");

    [Fact]
    public void CharacterExpr_LessEqual_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name <= "abc", "\"t\".name <= :0", 1, "abc");

    [Fact]
    public void CharacterExpr_GreaterEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.name >= L("abc"), "\"t\".name >= 'abc'");

    [Fact]
    public void CharacterExpr_GreaterEqual_ColumnAndChar_CorrectSql() =>
        _assert.Equal(_t.name >= 'a', "\"t\".name >= :0", 1, "a");

    [Fact]
    public void CharacterExpr_GreaterEqual_ColumnAndString_CorrectSql() =>
        _assert.Equal(_t.name >= "abc", "\"t\".name >= :0", 1, "abc");
}
