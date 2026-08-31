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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
    public void InsertInto_SetNoAssignments_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t).Set());

        Assert.Equal("SET requires at least one assignment.", ex.Message);
    }

    [Fact]
    public void InsertInto_SetWithNotEqual_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t).Set(t.Code != 1));

        Assert.Equal(
            "Invalid type for Assignment: SqlArtisan.Internal.NotEqualCondition",
            ex.Message);
    }

    [Fact]
    public void InsertInto_SetNullAssignment_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertInto(t).Set(t.Code == 1, null!));

        Assert.Equal(
            "A SET assignment list must not contain a null assignment. (Parameter 'assignments')",
            ex.Message);
    }

    [Fact]
    public void InsertInto_ValuesNoArguments_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t).Values());

        Assert.Equal("A VALUES row requires at least one value.", ex.Message);
    }

    [Fact]
    public void InsertInto_ValuesRowWidthExceedsColumnList_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code).Values(1, "a"));

        Assert.Equal(
            "The INSERT column list declares 1 column(s), but this VALUES row has 2 value(s).",
            ex.Message);
    }

    [Fact]
    public void InsertInto_ValuesRowWidthBelowColumnList_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name).Values(1));

        Assert.Equal(
            "The INSERT column list declares 2 column(s), but this VALUES row has 1 value(s).",
            ex.Message);
    }

    [Fact]
    public void InsertInto_EmptyColumnList_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, []));

        Assert.Equal("An INSERT column list requires at least one column.", ex.Message);
    }

    [Fact]
    public void InsertIgnoreInto_EmptyColumnList_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertIgnoreInto(t, []));

        Assert.Equal("An INSERT column list requires at least one column.", ex.Message);
    }

    [Fact]
    public void InsertInto_NullColumnElement_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertInto(t, t.Code, null!));

        Assert.Equal(
            "An INSERT column list must not contain a null column. (Parameter 'columns')",
            ex.Message);
    }

    [Fact]
    public void InsertIgnoreInto_NullColumnElement_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertIgnoreInto(t, t.Code, null!));

        Assert.Equal(
            "An INSERT column list must not contain a null column. (Parameter 'columns')",
            ex.Message);
    }

    [Fact]
    public void InsertIgnoreInto_SetNoAssignments_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertIgnoreInto(t).Set());

        Assert.Equal("SET requires at least one assignment.", ex.Message);
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

    [Fact]
    public void InsertInto_SqlServer_Output_CorrectSql()
    {
        // OUTPUT sits after the column list and before VALUES.
        TestTable t = new();

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Output(Inserted(t.Code))
            .Values(1, "x")
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("OUTPUT INSERTED.code ");
        expected.Append("VALUES (@0, @1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>("@0"));
        Assert.Equal("x", sql.Parameters.Get<string>("@1"));
    }

    [Fact]
    public void InsertInto_SqlServer_OutputInto_CorrectSql()
    {
        TestTable t = new();
        ArchiveTable a = new();

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Output(Inserted(t.Code), Inserted(t.Name))
            .Into(a, a.Code, a.Name)
            .Values(1, "x")
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("OUTPUT INSERTED.code, INSERTED.name ");
        expected.Append("INTO archive_table (code, name) ");
        expected.Append("VALUES (@0, @1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_SqlServer_OutputIntoAliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name)
            .Output(Inserted(t.Code), Inserted(t.Name))
            .Into(a, a.Code, a.Name));

        Assert.Equal(
            "The destination table of OUTPUT ... INTO must not be aliased.", ex.Message);
    }

    [Fact]
    public void InsertInto_SqlServer_OutputAndReturning_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code)
            .Output(Inserted(t.Code))
            .Values(1)
            .Returning(t.Code)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with RETURNING; use one or the other.", ex.Message);
    }

    [Fact]
    public void InsertInto_SqlServer_OutputIntoAndReturning_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code)
            .Output(Inserted(t.Code))
            .Into(a, a.Code)
            .Values(1)
            .Returning(t.Code)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with RETURNING; use one or the other.", ex.Message);
    }

    [Fact]
    public void InsertInto_SqlServer_OutputAndOnConflict_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code)
            .Output(Inserted(t.Code))
            .Values(1)
            .OnConflict(t.Code)
            .DoNothing()
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with ON CONFLICT or ON DUPLICATE KEY UPDATE; "
                + "use one or the other.",
            ex.Message);
    }

    [Fact]
    public void InsertInto_SqlServer_OutputAndOnDuplicateKeyUpdate_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name)
            .Output(Inserted(t.Code))
            .Values(1, "a")
            .OnDuplicateKeyUpdate(t.Name == "b")
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with ON CONFLICT or ON DUPLICATE KEY UPDATE; "
                + "use one or the other.",
            ex.Message);
    }

    [Fact]
    public void InsertIntoSet_NonColumnLeftSide_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t).Set(Abs(t.Code) == 5));

        Assert.Equal("The left side of a SET assignment must be a column.", ex.Message);
    }

    [Fact]
    public void InsertIntoSelect_WidthMismatch_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name).Select(s.Code).From(s).Build());

        Assert.Equal(
            "The INSERT column list declares 2 column(s), but the SELECT list has 1 item(s).",
            ex.Message);
    }

    [Fact]
    public void InsertIntoSelect_QualifiedStar_CorrectSql()
    {
        // A star's width is the schema's, not countable, so the width check
        // stays out of the way.
        TestTable t = new();
        TestTable s = new("s");

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name).Select(s.Asterisk).From(s).Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) SELECT \"s\".* FROM test_table \"s\"",
            sql.Text);
    }

    [Fact]
    public void InsertInto_MySql_AliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new("t");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code).Values(1).Build(Dbms.MySql));

        Assert.Equal(
            "MySQL does not support aliasing the target of an INSERT statement; "
                + "use an unaliased target table.",
            ex.Message);
    }

    [Fact]
    public void InsertIntoSelect_SqlServer_TopWithOffset_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code)
                .Select(Top(5), s.Code)
                .From(s)
                .OrderBy(s.Code)
                .OffsetRows(3)
                .Build(Dbms.SqlServer));

        Assert.Equal(
            "TOP cannot be combined with OFFSET / FETCH on SQL Server; use one or the other.",
            ex.Message);
    }

    [Fact]
    public void InsertIntoSelect_SqlServer_TopWithTiesWithoutOrderBy_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code)
                .Select(Top(5).WithTies(), s.Code)
                .From(s)
                .Build(Dbms.SqlServer));

        Assert.Equal("TOP ... WITH TIES requires an ORDER BY clause.", ex.Message);
    }
}
