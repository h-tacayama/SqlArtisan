using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class InsertTests
{
    [Fact]
    public void InsertInto_WithoutColumnList_SqlWithValuesOnly()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t)
            .Values(1, "a", Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append("SYSDATE");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_NullValue_EmitsNullLiteral()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, null!)
            .Build();

        Assert.Equal("INSERT INTO test_table (code, name) VALUES (:0, NULL)", sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
    }

    [Fact]
    public void InsertInto_WithColumnList_SqlWithColumnsAndValues()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Values(1, "a", Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append("SYSDATE");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithNull_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Values(Null, Null, Null)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append("NULL, ");
        expected.Append("NULL, ");
        expected.Append("NULL");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithSetClause_SqlWithColumnsAndValues()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t)
            .Set(
                t.Code == 1,
                t.Name == "a",
                t.CreatedAt == Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append("SYSDATE");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithAlias_CorrectSql()
    {
        TestTable t = new("t");
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table AS \"t\" ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append(":0, ");
        expected.Append(":1");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_Oracle_WithAlias_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933): the alias follows the
        // table name with only a space.
        TestTable t = new("t");
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table \"t\" ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append(":0, ");
        expected.Append(":1");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithSelectClause_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Select(s.Code, s.Name, s.CreatedAt)
            .From(s)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table AS \"t\" ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("SELECT ");
        expected.Append("\"s\".code, ");
        expected.Append("\"s\".name, ");
        expected.Append("\"s\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_SelectFromTargetTable_CorrectSql()
    {
        // INSERT..SELECT reading the insert target is legal — the correlated-DML
        // guard (#253) arms only for UPDATE/DELETE.
        TestTable t = new();

        SqlStatement sql =
            InsertInto(t, t.Code)
            .Select(t.Code)
            .From(t)
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code) SELECT code FROM test_table",
            sql.Text);
    }

    [Fact]
    public void InsertInto_SqlServer_AliasedTarget_ThrowsArgumentException()
    {
        // An aliased INSERT target is valid on PostgreSQL (it is how ON CONFLICT
        // is written), but T-SQL cannot alias the target directly, so the aliased
        // form has no valid spelling on SQL Server — the guard throws at Build
        // (ADR 0011).
        TestTable t = new("cu");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name).Values(1, "a").Build(Dbms.SqlServer));

        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE statement; use an unaliased target table.",
            ex.Message);
    }

    [Fact]
    public void InsertInto_SqlServer_UnaliasedTarget_CorrectSql()
    {
        // The unaliased target builds normally on SQL Server — only the alias is
        // rejected. Parameters use the @ marker.
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append("@0, ");
        expected.Append("@1");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertIgnoreInto_MySql_WithColumnList_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertIgnoreInto(t, t.Code, t.Name)
            .Values(1, "a")
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("INSERT IGNORE INTO ");
        expected.Append("test_table ");
        expected.Append('(');
        expected.Append("code, ");
        expected.Append("name");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append('(');
        expected.Append("?0, ");
        expected.Append("?1");
        expected.Append(')');

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
    }

    [Fact]
    public void InsertIgnoreInto_MySql_WithoutColumnList_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertIgnoreInto(t)
            .Values(1, "a")
            .Build(Dbms.MySql);

        Assert.Equal("INSERT IGNORE INTO test_table VALUES (?0, ?1)", sql.Text);
    }

    [Fact]
    public void InsertIgnoreInto_MySql_MultipleRows_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertIgnoreInto(t, t.Code, t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT IGNORE INTO test_table (code, name) VALUES (?0, ?1), (?2, ?3)",
            sql.Text);
    }

    [Fact]
    public void InsertIgnoreInto_MySql_WithSetClause_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertIgnoreInto(t)
            .Set(t.Code == 1, t.Name == "a")
            .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT IGNORE INTO test_table (code, name) VALUES (?0, ?1)",
            sql.Text);
    }

    [Fact]
    public void InsertIgnoreInto_MySql_WithSelectClause_CorrectSql()
    {
        TestTable t = new();
        TestTable s = new();
        SqlStatement sql =
            InsertIgnoreInto(t, t.Code, t.Name)
            .Select(s.Code, s.Name)
            .From(s)
            .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT IGNORE INTO test_table (code, name) SELECT code, name FROM test_table",
            sql.Text);
    }
}
