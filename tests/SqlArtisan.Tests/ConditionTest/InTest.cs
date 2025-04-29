using System.Text;

namespace InlineSqlSharp.Tests;

public class InTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public InTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void In_SingleInt_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(_t.Code.In(2), expected.ToString(), 1, 2);
    }

    [Fact]
    public void In_MultipleInts_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.Code.In(1, 2, 3),
            expected.ToString(),
            3, 1, 2, 3);
    }

    [Fact]
    public void NotIn_MultipleEnums_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.Code.NotIn(TestEnum.One, TestEnum.Two, TestEnum.Three),
            expected.ToString(),
            3, TestEnum.One, TestEnum.Two, TestEnum.Three);
    }
}
