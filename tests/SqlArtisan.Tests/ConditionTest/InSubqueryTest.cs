using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class InSubqueryTest
{
    private readonly TestTable _t;
    private readonly TestTable _s;
    private readonly ConditionTestAssert _assert;

    public InSubqueryTest()
    {
        _t = new TestTable("t");
        _s = new TestTable("s");
        _assert = new(_t);
    }

    [Fact]
    public void In_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.Code.In(Select(_s.Code).From(_s)),
            expected.ToString());
    }

    [Fact]
    public void NotIn_Subquery_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code NOT IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.Code.NotIn(Select(_s.Code).From(_s)),
            expected.ToString());
    }
}
