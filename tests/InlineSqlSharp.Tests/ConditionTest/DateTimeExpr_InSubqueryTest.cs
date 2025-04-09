using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DateTimeExpr_InSubqueryTest
{
    private readonly test_table _t;
    private readonly test_table _s;
    private readonly ConditionTestAssert _assert;

    public DateTimeExpr_InSubqueryTest()
    {
        _t = new test_table("t");
        _s = new test_table("s");
        _assert = new(_t);
    }

    [Fact]
    public void DateTimeExpr_IN_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.IN(SELECT(_s.created_at).FROM(_s)),
            expected.ToString());
    }

    [Fact]
    public void DateTimeExpr_NOT_IN_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".created_at NOT IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.created_at.NOT_IN(SELECT(_s.created_at).FROM(_s)),
            expected.ToString());
    }
}
