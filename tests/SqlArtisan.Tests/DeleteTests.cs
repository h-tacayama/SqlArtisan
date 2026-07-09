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
