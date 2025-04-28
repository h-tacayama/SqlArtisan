using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SubqueryTest
{
    private readonly TestTable _t;
    private readonly TestTable _s;
    private readonly TestTable _r;
    private readonly ConditionTestAssert _assert;

    public SubqueryTest()
    {
        _t = new TestTable("t");
        _s = new TestTable("s");
        _r = new TestTable("r");
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
            _t.Code.In(Select(_s.Code)),
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
            _t.Code.In(Select(_s.Code).From(_s)),
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
            _t.Code.In(Select(Distinct, _s.Code).From(_s)),
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
        expected.Append("\"s\".code > :0");
        expected.Append(")");

        _assert.Equal(
            _t.Code.In(Select(_s.Code).From(_s).Where(_s.Code > 2)),
            expected.ToString(),
            1, 2);
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
            _t.Code.In(Select(_s.Code).From(_s).InnerJoin(_r).On(_s.Code == _r.Code)),
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
            _t.Code.In(Select(Hints("/*+ ANY HINT */"), _s.Code)),
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
            _t.Code.In(Select(Hints("/*+ ANY HINT */"), Distinct, _s.Code)),
            expected.ToString());
    }
}
