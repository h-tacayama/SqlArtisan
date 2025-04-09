using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SubqueryTest
{
    private readonly test_table _t;
    private readonly test_table _s;
    private readonly test_table _r;
    private readonly ConditionTestAssert _assert;

    public SubqueryTest()
    {
        _t = new test_table("t");
        _s = new test_table("s");
        _r = new test_table("r");
        _assert = new(_t);
    }

    [Fact]
    public void Subquery_SimpleSelect_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".code");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(_s.code)),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectWithFrom_CorrectSql()
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
            _t.code.IN(SELECT(_s.code).FROM(_s)),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectDistinctWithFrom_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("DISTINCT ");
        expected.Append("\"s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(DISTINCT, _s.code).FROM(_s)),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectWithFromAndWhere_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\" ");
        expected.Append("WHERE ");
        expected.Append("\"s\".code > 1");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(_s.code).FROM(_s).WHERE(_s.code > L(1))),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectWithFromAndJoin_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\" ");
        expected.Append("INNER JOIN ");
        expected.Append("test_table \"r\" ");
        expected.Append("ON ");
        expected.Append("\"s\".code = \"r\".code");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(_s.code).FROM(_s).INNER_JOIN(_r).ON(_s.code == _r.code)),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectWithHints_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("\"s\".code");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(HINTS("/*+ ANY HINT */"), _s.code)),
            expected.ToString());
    }

    [Fact]
    public void Subquery_SelectWithHintsAndDistinct_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("DISTINCT ");
        expected.Append("\"s\".code");
        expected.Append(")");

        _assert.Equal(
            _t.code.IN(SELECT(HINTS("/*+ ANY HINT */"), DISTINCT, _s.code)),
            expected.ToString());
    }
}
