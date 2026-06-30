using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class SubqueryTests
{
    private readonly TestTable _t;
    private readonly TestTable _s;
    private readonly TestTable _r;
    private readonly ConditionTestAssert _assert;

    public SubqueryTests()
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

    [Fact]
    public void ScalarSubquery_InSelectList_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Name,
                Select(Max(_s.Code)).From(_s))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, ");
        expected.Append("(SELECT MAX(\"s\".code) FROM test_table \"s\") ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ScalarSubquery_WithAlias_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Name,
                Select(Max(_s.Code)).From(_s).As("max_code"))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, ");
        expected.Append("(SELECT MAX(\"s\".code) FROM test_table \"s\") \"max_code\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ScalarSubquery_InWhereComparison_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > Select(Avg(_s.Code)).From(_s))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > (SELECT AVG(\"s\".code) FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ScalarSubquery_InArithmetic_CorrectSql()
    {
        SqlStatement sql =
            Select(
                (_t.Code - Select(Avg(_s.Code)).From(_s)).As("diff"))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(\"t\".code - (SELECT AVG(\"s\".code) FROM test_table \"s\")) \"diff\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ScalarSubquery_Correlated_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > Select(Max(_s.Code)).From(_s).Where(_s.Name == _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ");
        expected.Append("(SELECT MAX(\"s\".code) FROM test_table \"s\" ");
        expected.Append("WHERE \"s\".name = \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ScalarSubquery_WithBindParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > Select(Max(_s.Code)).From(_s).Where(_s.Code > 10))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ");
        expected.Append("(SELECT MAX(\"s\".code) FROM test_table \"s\" ");
        expected.Append("WHERE \"s\".code > :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(10, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void QuantifiedSubquery_Any_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > Any(Select(_s.Code).From(_s)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ANY (SELECT \"s\".code FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QuantifiedSubquery_All_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > All(Select(_s.Code).From(_s)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ALL (SELECT \"s\".code FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QuantifiedSubquery_Some_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == Some(Select(_s.Code).From(_s)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = SOME (SELECT \"s\".code FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QuantifiedSubquery_All_WithBindParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > All(Select(_s.Code).From(_s).Where(_s.Code > 10)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ALL (SELECT \"s\".code FROM test_table \"s\" ");
        expected.Append("WHERE \"s\".code > :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(10, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void QuantifiedSubquery_Any_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > Any(Select(_s.Code).From(_s)))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("WHERE ");
        expected.Append("`t`.code > ANY (SELECT `s`.code FROM test_table `s`)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QuantifiedSubquery_All_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code > All(Select(_s.Code).From(_s)))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code > ALL (SELECT \"s\".code FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
