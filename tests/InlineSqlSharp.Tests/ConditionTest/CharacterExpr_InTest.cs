using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CharacterExpr_InTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public CharacterExpr_InTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void CharacterExpr_IN_SingleLiteral_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name IN ");
        expected.Append("(");
        expected.Append("'a'");
        expected.Append(")");

        _assert.Equal(_t.name.IN(L("a")), expected.ToString());
    }

    [Fact]
    public void CharacterExpr_IN_MultipleChars_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.name.IN('a', 'b', 'c'),
            expected.ToString(),
            3, "a", "b", "c");
    }

    [Fact]
    public void CharacterExpr_IN_MultipleStrings_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.name.IN("a", "b", "c"),
            expected.ToString(),
            3, "a", "b", "c");
    }

    [Fact]
    public void CharacterExpr_NOT_IN_SingleLiteral_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name NOT IN ");
        expected.Append("(");
        expected.Append("'a'");
        expected.Append(")");

        _assert.Equal(_t.name.NOT_IN(L("a")), expected.ToString());
    }

    [Fact]
    public void CharacterExpr_NOT_IN_MultipleChars_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.name.NOT_IN('a', 'b', 'c'),
            expected.ToString(),
            3, "a", "b", "c");
    }

    [Fact]
    public void CharacterExpr_NOT_IN_MultipleStrings_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.name.NOT_IN("a", "b", "c"),
            expected.ToString(),
            3, "a", "b", "c");
    }
}
