using SqlArtisan.Databases.Sqlite;
using SqliteSql = SqlArtisan.Databases.Sqlite.Sql;

namespace SqlArtisan.Tests;

// Extension-based ③ for UPSERT, SQLite. Same OnConflict surface as PostgreSQL but
// the lowercase `excluded` spelling falls out of the SqliteQuery terminal at
// Build. Kept in its own file because importing both the PostgreSql and Sqlite
// namespaces would make the duplicated OnConflict extension ambiguous — the
// extension approach's residual multi-import cost.
public class UpsertNamespaceSqliteTests
{
    [Fact]
    public void OnConflict_DoUpdateSet_BuildsWithLowercaseExcluded()
    {
        TestTable t = new();
        SqlStatement sql =
            SqliteSql.InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name)
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1) " +
            "ON CONFLICT (code) DO UPDATE SET name = excluded.name",
            sql.Text);
    }
}
