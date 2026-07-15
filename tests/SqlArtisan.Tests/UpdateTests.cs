using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class UpdateTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Update_SetLiterals_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == 1,
                _t.Name == "a",
                _t.CreatedAt == Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("code = :0, ");
        expected.Append("name = :1, ");
        expected.Append("created_at = SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SetMultipleVariables_CorrectSql()
    {
        TestTableDto dto = new(1, "Test1", new DateTime(2001, 2, 3));

        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == dto.Code,
                _t.Name == dto.Name,
                _t.CreatedAt == dto.CreatedAt)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("code = :0, ");
        expected.Append("name = :1, ");
        expected.Append("created_at = :2");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Test1", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
    }

    [Fact]
    public void Update_WithAlias_CorrectSql()
    {
        // An aliased UPDATE declares the alias with AS on the target; the SET
        // left side stays unqualified (PostgreSQL rejects `SET x.col = ...`),
        // while the WHERE predicate qualifies through the alias.
        TestTable t = new("t");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == "a")
            .Where(t.Code == 1)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SqlServer_AliasedTarget_ThrowsArgumentException()
    {
        // T-SQL cannot alias the UPDATE target directly, so the aliased form has
        // no valid spelling on SQL Server — the guard throws at Build (ADR 0011).
        TestTable t = new("cu");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(t).Set(t.Name == "a").Where(t.Code == 1).Build(Dbms.SqlServer));

        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table.",
            ex.Message);
    }

    [Fact]
    public void Update_SqlServer_UnaliasedTarget_CorrectSql()
    {
        // The unaliased target builds normally on SQL Server — only the alias is
        // rejected. Columns render unqualified; parameters use the @ marker.
        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "a")
            .Where(_t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("name = @0 ");
        expected.Append("WHERE ");
        expected.Append("code = @1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_Oracle_WithAlias_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933): the alias follows the
        // table name with only a space.
        TestTable t = new("t");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == "a")
            .Where(t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SetWithInequality_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Update(_t)
            .Set(
                _t.Code != 1)
            .Build();
        });
    }

    [Fact]
    public void Update_WhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(_t)
            .Set(_t.Name == "x")
            .Where(ConditionIf(false, _t.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void Update_NoWhere_CorrectSql()
    {
        // The legal twin: omitting WHERE is the intentional full-table UPDATE — it must still build.
        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "x")
            .Build();

        Assert.Equal("UPDATE test_table SET name = :0", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Update_CorrelatedSubqueryUnaliasedTarget_ThrowsArgumentException()
    {
        // The bare outer column would resolve to the inner table — a silent
        // tautology updating every row — so the guard throws at Build (#253).
        TestTable r = new("r");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(_t)
            .Set(_t.Code == Select(Max(r.Code)).From(r).Where(r.Name == _t.Name))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE or DELETE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void Update_CorrelatedSubqueryAliasedTarget_CorrectSql()
    {
        TestTable c = new("c");
        TestTable r = new("r");

        SqlStatement sql =
            Update(c)
            .Set(c.Code == Select(Max(r.Code)).From(r).Where(r.Name == c.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table AS \"c\" ");
        expected.Append("SET ");
        expected.Append("code = ");
        expected.Append("(SELECT MAX(\"r\".code) ");
        expected.Append("FROM test_table \"r\" ");
        expected.Append("WHERE \"r\".name = \"c\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_UncorrelatedSubqueryUnaliasedTarget_CorrectSql()
    {
        TestTable s = new("s");

        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "x")
            .Where(_t.Code.In(Select(s.Code).From(s)))
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("code IN ");
        expected.Append("(SELECT \"s\".code FROM test_table \"s\")");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Update_TargetColumnAfterSubquery_CorrectSql()
    {
        // A bare target column following an uncorrelated subquery is back at
        // statement scope — pins the guard's boundary tracking.
        TestTable s = new("s");

        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "x")
            .Where(_t.Code.In(Select(s.Code).From(s)) & (_t.Code == 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("(code IN (SELECT \"s\".code FROM test_table \"s\")) ");
        expected.Append("AND (code = :1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
        Assert.Equal(1, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Update_EmptyConditionBesideActiveCondition_CorrectSql()
    {
        // Empty at the call but made non-empty by a later `&` — the guard runs at Build(), not eagerly.
        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "x")
            .Where(ConditionIf(false, _t.Code > 0) & (_t.Code == 1))
            .Build();

        Assert.Equal("UPDATE test_table SET name = :0 WHERE (code = :1)", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
        Assert.Equal(1, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Update_PostgreSql_From_CorrectSql()
    {
        // PostgreSQL UPDATE ... FROM: the target is not repeated, the SET target
        // stays unqualified, and the join predicate lives in WHERE.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == s.Name)
            .From(s)
            .Where(t.Code == s.Code)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = \"s\".name ");
        expected.Append("FROM test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_PostgreSql_FromLiteral_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == "x")
            .From(s)
            .Where(t.Code == s.Code)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = :0 ");
        expected.Append("FROM test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Update_PostgreSql_FromWithReturning_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == s.Name)
            .From(s)
            .Where(t.Code == s.Code)
            .Returning(t.Code)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = \"s\".name ");
        expected.Append("FROM test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code ");
        expected.Append("RETURNING \"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_Sqlite_From_CorrectSql()
    {
        // SQLite's UPDATE ... FROM (3.33+) shares PostgreSQL's spelling.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == s.Name)
            .From(s)
            .Where(t.Code == s.Code)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = \"s\".name ");
        expected.Append("FROM test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SqlServer_FromRepeatedTarget_CorrectSql()
    {
        // SQL Server leads with the FROM-defined alias alone, re-lists the target
        // in FROM, joins the auxiliary table, and qualifies the SET target.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == s.Name)
            .From(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("UPDATE \"t\" ");
        expected.Append("SET \"t\".name = \"s\".name ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("INNER JOIN test_table \"s\" ");
        expected.Append("ON \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_MySql_Join_CorrectSql()
    {
        // MySQL's multi-table UPDATE puts the JOIN before SET and qualifies the
        // SET target; the target keeps its full `table AS alias` lead.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .Set(t.Name == s.Name)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS `t` ");
        expected.Append("INNER JOIN test_table `s` ");
        expected.Append("ON `t`.code = `s`.code ");
        expected.Append("SET `t`.name = `s`.name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_Join_UnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(t).InnerJoin(s).On(t.Code == s.Code).Set(t.Name == s.Name).Build(Dbms.MySql));

        Assert.Equal(
            "The target of a joined UPDATE or DELETE must be aliased.",
            ex.Message);
    }
}
