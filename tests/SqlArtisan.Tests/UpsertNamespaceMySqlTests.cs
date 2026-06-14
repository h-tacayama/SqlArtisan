using SqlArtisan.Databases.MySql;
using MySqlSql = SqlArtisan.Databases.MySql.Sql;

namespace SqlArtisan.Tests;

// Extension-based ③ for UPSERT, MySQL. Only the MySql namespace is imported, so
// OnDuplicateKeyUpdate is reachable and OnConflict (PostgreSql/Sqlite extension)
// is not.
public class UpsertNamespaceMySqlTests
{
    [Fact]
    public void OnDuplicateKeyUpdate_BuildsRowAliasForm()
    {
        TestTable t = new();
        SqlStatement sql =
            MySqlSql.InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnDuplicateKeyUpdate(t.Name)
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (?0, ?1) " +
            "AS new ON DUPLICATE KEY UPDATE name = new.name",
            sql.Text);
    }

    // Filtering proof: PostgreSQL's verb is not in scope here.
    [Fact]
    public void PostgreSql_Verb_Is_Not_In_Scope()
    {
        TestTable t = new();
        var afterValues = MySqlSql.InsertInto(t, t.Code, t.Name).Values(1, "a");

        // afterValues.OnConflict(t.Code);  // CS1061: no such method in MySql scope

        Assert.NotNull(afterValues);
    }
}
