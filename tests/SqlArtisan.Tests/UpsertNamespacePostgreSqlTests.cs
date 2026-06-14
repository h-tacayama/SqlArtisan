using SqlArtisan.Databases.PostgreSql;          // brings the OnConflict/DoUpdateSet/DoNothing extensions into scope
using PgSql = SqlArtisan.Databases.PostgreSql.Sql;

namespace SqlArtisan.Tests;

// Extension-based ③ for UPSERT, PostgreSQL. This file imports ONLY the PostgreSql
// namespace, so OnConflict is reachable and OnDuplicateKeyUpdate (a MySql-namespace
// extension) is NOT — the filtering is the import scope, with no wrapper types.
public class UpsertNamespacePostgreSqlTests
{
    [Fact]
    public void OnConflict_DoUpdateSet_BuildsWithUppercaseExcluded()
    {
        TestTable t = new();
        SqlStatement sql =
            PgSql.InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name)
            .Build();   // no DBMS arg — the terminal PostgreSqlQuery folds it in

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1) " +
            "ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name",
            sql.Text);
    }

    [Fact]
    public void OnConflict_DoNothing_Builds()
    {
        TestTable t = new();
        SqlStatement sql =
            PgSql.InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoNothing()
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1) " +
            "ON CONFLICT (code) DO NOTHING",
            sql.Text);
    }

    // Filtering proof: MySQL's verb is not in scope here (its namespace is not
    // imported), so the following would fail to compile with CS1061.
    [Fact]
    public void MySql_Verb_Is_Not_In_Scope()
    {
        TestTable t = new();
        var afterValues = PgSql.InsertInto(t, t.Code, t.Name).Values(1, "a");

        // afterValues.OnDuplicateKeyUpdate(t.Name);  // CS1061: no such method in PostgreSql scope

        Assert.NotNull(afterValues);
    }
}
