using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CharacterExpr_InSubqueryTest
{
    private readonly test_table _t;
    private readonly test_table _s;
    private readonly ConditionTestAssert _assert;

    public CharacterExpr_InSubqueryTest()
    {
        _t = new test_table("t");
        _s = new test_table("s");
        _assert = new(_t);
    }

    [Fact]
    public void CharacterExpr_IN_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".name IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\" ");
        expected.Append("WHERE ");
        expected.Append("\"s\".code = :1");
        expected.Append(")");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(")");

        _assert.Equal(
            AND(
                _t.code == P(1),
                _t.name.IN(SELECT(_s.name).FROM(_s).WHERE(_s.code == P(2))),
                _t.code == P(3)),
            expected.ToString(),
            3, 1, 2, 3);
    }

    [Fact]
    public void CharacterExpr_NOT_IN_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name NOT IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.name.NOT_IN(SELECT(_s.name).FROM(_s)),
            expected.ToString());
    }
}
