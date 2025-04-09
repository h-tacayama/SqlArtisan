using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DateTimeExpr_InTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public DateTimeExpr_InTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void DateTimeExpr_IN_SingleParameter_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at IN ");
        expected.Append("(");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.IN(P(new DateTime(2001, 2, 3))),
            expected.ToString(),
            1, new DateTime(2001, 2, 3));
    }

    [Fact]
    public void DateTimeExpr_IN_MultipleValues_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.IN(
            new DateTime(2001, 2, 3),
            new DateTime(2001, 2, 4),
            new DateTime(2001, 2, 5)),
            expected.ToString(),
            3,
            new DateTime(2001, 2, 3),
            new DateTime(2001, 2, 4),
            new DateTime(2001, 2, 5));
    }

    [Fact]
    public void DateTimeExpr_NOT_IN_SingleParameter_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at NOT IN ");
        expected.Append("(");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.NOT_IN(P(new DateTime(2001, 2, 3))),
            expected.ToString(),
            1, new DateTime(2001, 2, 3));
    }

    [Fact]
    public void DateTimeExpr_NOT_IN_MultipleValues_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.NOT_IN(
            new DateTime(2001, 2, 3),
            new DateTime(2001, 2, 4),
            new DateTime(2001, 2, 5)),
            expected.ToString(),
            3,
            new DateTime(2001, 2, 3),
            new DateTime(2001, 2, 4),
            new DateTime(2001, 2, 5));
    }
}
