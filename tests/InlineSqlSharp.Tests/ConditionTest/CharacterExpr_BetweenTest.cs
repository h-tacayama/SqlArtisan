using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CharacterExpr_BetweenTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public CharacterExpr_BetweenTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void CharacterExpr_BETWEEN_Literals_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN(L("a"), L("z")),
            "\"t\".name BETWEEN 'a' AND 'z'");

    [Fact]
    public void CharacterExpr_BETWEEN_LiteralAndChar_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN(L('a'), 'z'),
            "\"t\".name BETWEEN 'a' AND :0",
            1, "z");

    [Fact]
    public void CharacterExpr_BETWEEN_CharAndLiteral_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN('a', L('z')),
            "\"t\".name BETWEEN :0 AND 'z'",
            1, "a");

    [Fact]
    public void CharacterExpr_BETWEEN_Chars_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN('a', 'z'),
            "\"t\".name BETWEEN :0 AND :1",
            2, "a", "z");

    [Fact]
    public void CharacterExpr_BETWEEN_LiteralAndString_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN(L("a"), "z"),
            "\"t\".name BETWEEN 'a' AND :0",
            1, "z");

    [Fact]
    public void CharacterExpr_BETWEEN_StringAndLiteral_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN("a", L("z")),
            "\"t\".name BETWEEN :0 AND 'z'",
            1, "a");

    [Fact]
    public void CharacterExpr_BETWEEN_Strings_CorrectSql() =>
        _assert.Equal(
            _t.name.BETWEEN("a", "z"),
            "\"t\".name BETWEEN :0 AND :1",
            2, "a", "z");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_Literals_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN(L("a"), L("z")),
            "\"t\".name NOT BETWEEN 'a' AND 'z'");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_LiteralAndChar_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN(L('a'), 'z'),
            "\"t\".name NOT BETWEEN 'a' AND :0",
            1, "z");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_CharAndLiteral_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN('a', L('z')),
            "\"t\".name NOT BETWEEN :0 AND 'z'",
            1, "a");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_Chars_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN('a', 'z'),
            "\"t\".name NOT BETWEEN :0 AND :1",
            2, "a", "z");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_LiteralAndString_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN(L("a"), "z"),
            "\"t\".name NOT BETWEEN 'a' AND :0",
            1, "z");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_StringAndLiteral_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN("a", L("z")),
            "\"t\".name NOT BETWEEN :0 AND 'z'",
            1, "a");

    [Fact]
    public void CharacterExpr_NOT_BETWEEN_Strings_CorrectSql() =>
        _assert.Equal(
            _t.name.NOT_BETWEEN("a", "z"),
            "\"t\".name NOT BETWEEN :0 AND :1",
            2, "a", "z");
}
