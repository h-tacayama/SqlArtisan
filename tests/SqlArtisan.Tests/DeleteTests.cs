using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class DeleteTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void DeleteFrom_SimpleTable_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_WithWhereClause_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_Oracle_WithWhereClause_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933), so the alias follows
        // the table name with only a space.
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SqlServer_AliasedTarget_ThrowsArgumentException()
    {
        // T-SQL cannot alias the DELETE target directly, so the aliased form has
        // no valid spelling on SQL Server — the guard throws at Build (ADR 0011).
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t).Where(_t.Code == 1).Build(Dbms.SqlServer));

        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_UnaliasedTarget_CorrectSql()
    {
        // The unaliased target builds normally on SQL Server — only the alias is
        // rejected. Columns render unqualified; parameters use the @ marker.
        TestTable t = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Where(t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE ");
        expected.Append("code = @0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_CorrelatedSubqueryUnaliasedTarget_ThrowsArgumentException()
    {
        // The bare outer column would resolve to the inner table — a silent
        // tautology deleting every row — so the guard throws at Build (#253).
        TestTable t = new();
        TestTable r = new("r");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(Select(r.Code).From(r).Where(r.Code == t.Code)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE or DELETE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CorrelatedSubqueryAliasedTarget_CorrectSql()
    {
        TestTable r = new("r");

        SqlStatement sql =
            DeleteFrom(_t)
            .Where(Exists(Select(r.Code).From(r).Where(r.Code == _t.Code)))
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("WHERE ");
        expected.Append("EXISTS ");
        expected.Append("(SELECT \"r\".code ");
        expected.Append("FROM test_table \"r\" ");
        expected.Append("WHERE \"r\".code = \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SameTableInstanceInSubquery_ThrowsArgumentException()
    {
        // Accepted false positive: one C# instance standing for two SQL scopes is
        // ambiguous authorship — use a second instance for the inner scope, or alias the target.
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(t.Code.In(Select(t.Code).From(t)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE or DELETE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CteBodyReferencingTarget_CorrectSql()
    {
        // A CTE body cannot correlate with the DML target — its reference
        // resolves in the CTE's own FROM — so the guard leaves it alone.
        TestTable t = new();
        TestCte cte = new("cte");

        SqlStatement sql =
            With(cte.As(Select(t.Code.As(cte.CteCode)).From(t)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT code cte_code FROM test_table) ");
        expected.Append("DELETE FROM test_table ");
        expected.Append("WHERE code IN ");
        expected.Append("(SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Delete_WhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Where(ConditionIf(false, _t.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }
}
