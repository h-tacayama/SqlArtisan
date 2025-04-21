using System.Text;

namespace InlineSqlSharp.Tests;

public class InTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public InTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void IN_SingleInt_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(_t.code.IN(2), expected.ToString(), 1, 2);
    }

    [Fact]
    public void IN_MultipleInts_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.code.IN(1, 2, 3),
            expected.ToString(),
            3, 1, 2, 3);
    }

    [Fact]
    public void NOT_IN_MultipleEnums_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.code.NOT_IN(TestEnum.One, TestEnum.Two, TestEnum.Three),
            expected.ToString(),
            3, TestEnum.One, TestEnum.Two, TestEnum.Three);
    }
}
