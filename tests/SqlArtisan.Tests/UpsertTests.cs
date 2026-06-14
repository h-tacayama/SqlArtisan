using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// #85 UPSERT — the neutral fluent API (Approach A): both ON CONFLICT and
// ON DUPLICATE KEY UPDATE are visible on one builder; the DBMS is chosen at
// Build(). These tests pin the exact per-dialect SQL.
public class UpsertTests
{
    [Fact]
    public void OnConflictDoUpdate_PostgreSql_UsesUppercaseExcluded()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflictDoUpdate_Sqlite_UsesLowercaseExcluded()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) DO UPDATE SET name = excluded.name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnConflictDoNothing_PostgreSql_EmitsDoNothing()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoNothing()
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) DO NOTHING");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OnDuplicateKeyUpdate_MySql_UsesRowAliasForm()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnDuplicateKeyUpdate(t.Name)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (?0, ?1) ");
        expected.Append("AS new ON DUPLICATE KEY UPDATE name = new.name");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
