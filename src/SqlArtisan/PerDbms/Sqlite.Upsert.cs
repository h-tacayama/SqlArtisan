using SqlArtisan.Internal;

namespace SqlArtisan.Databases.Sqlite;

// SQLite also uses ON CONFLICT — so its verb extensions are a near-duplicate of
// PostgreSQL's, differing only in the terminal type (SqliteQuery, for the
// lowercase `excluded` spelling resolved at Build). This duplication is the
// extension approach's residual cost: a shared OnConflict cannot serve both
// because each must return its own DBMS terminal for the no-arg Build. Importing
// BOTH the PostgreSql and Sqlite namespaces in one file makes OnConflict
// ambiguous — a real downside recorded in the write-up.
public static partial class Sql
{
    public static IExtUpsertColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new ExtUpsertBuilder(new InsertBuilder(new InsertIntoClause(table, columns)));
}

public static class SqliteUpsertExtensions
{
    public static IExtConflictAction OnConflict(this IExtUpsertValues values, params DbColumn[] conflictTarget)
    {
        ((ExtUpsertBuilder)values).Inner.OnConflict(conflictTarget);
        return (ExtUpsertBuilder)values;
    }

    public static SqliteQuery DoUpdateSet(this IExtConflictAction action, params DbColumn[] updateColumns) =>
        new(((ExtUpsertBuilder)action).Inner.DoUpdateSet(updateColumns));

    public static SqliteQuery DoNothing(this IExtConflictAction action) =>
        new(((ExtUpsertBuilder)action).Inner.DoNothing());
}
