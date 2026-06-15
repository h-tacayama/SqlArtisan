using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class UpsertTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void OnConflict_DoUpdateSet_PostgreSql_UsesUppercaseExcluded()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = EXCLUDED.name");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoUpdateSet_Sqlite_UsesLowercaseExcluded()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = excluded.name");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Build(Dbms.Sqlite);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoUpdateSet_MultipleTargetColumns_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name, created_at) ");
        expected.Append("VALUES (:0, :1, SYSDATE) ");
        expected.Append("ON CONFLICT (code, name) ");
        expected.Append("DO UPDATE SET created_at = EXCLUDED.created_at");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
            .Values(1, "a", Sysdate)
            .OnConflict(_t.Code, _t.Name)
            .DoUpdateSet(_t.CreatedAt == Excluded(_t.CreatedAt))
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoUpdateSet_WithWhere_CorrectSql()
    {
        // Arrange
        // In DO UPDATE ... WHERE, both the target row and EXCLUDED are in scope,
        // so a target column must be qualified with the table name; PostgreSQL
        // rejects an unqualified column here as ambiguous.
        DbColumn targetCode = new("test_table", "code");

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = EXCLUDED.name ");
        expected.Append("WHERE \"test_table\".code < :2");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Where(targetCode < 100)
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoNothing_WithTarget_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO NOTHING");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict(_t.Code)
            .DoNothing()
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoNothing_WithoutTarget_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT ");
        expected.Append("DO NOTHING");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict()
            .DoNothing()
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoUpdateSet_WithReturning_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = EXCLUDED.name ");
        expected.Append("RETURNING code");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Returning(_t.Code)
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnDuplicateKeyUpdate_MySql_UsesRowAlias()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (?0, ?1) ");
        expected.Append("AS new ");
        expected.Append("ON DUPLICATE KEY UPDATE name = new.name");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .OnDuplicateKeyUpdate(_t.Name == Excluded(_t.Name))
            .Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnDuplicateKeyUpdate_MySql_MultipleAssignments_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name, created_at) ");
        expected.Append("VALUES (?0, ?1, SYSDATE) ");
        expected.Append("AS new ");
        expected.Append("ON DUPLICATE KEY UPDATE name = new.name, created_at = new.created_at");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
            .Values(1, "a", Sysdate)
            .OnDuplicateKeyUpdate(
                _t.Name == Excluded(_t.Name),
                _t.CreatedAt == Excluded(_t.CreatedAt))
            .Build(Dbms.MySql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflict_DoUpdateSet_MultiRowValues_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1), (:2, :3) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = EXCLUDED.name");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }
}
