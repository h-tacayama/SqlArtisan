using SqlArtisan.Internal;

namespace SqlArtisan.Databases.PostgreSql;

// EXTENSION-based ③ for UPSERT (replaces the earlier per-DBMS wrapper version).
// The entry is gated per namespace; the verbs are namespace-scoped extension
// methods on the shared verb-less surface (IExtUpsertValues / IExtConflictAction),
// so `using SqlArtisan.Databases.PostgreSql;` surfaces OnConflict but never MySQL's
// OnDuplicateKeyUpdate — with NO per-DBMS wrapper state machine. Terminals return
// PostgreSqlQuery so Build() folds the DBMS with no argument.
public static partial class Sql
{
    public static IExtUpsertColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new ExtUpsertBuilder(new InsertBuilder(new InsertIntoClause(table, columns)));
}

public static class PostgreSqlUpsertExtensions
{
    public static IExtConflictAction OnConflict(this IExtUpsertValues values, params DbColumn[] conflictTarget)
    {
        ((ExtUpsertBuilder)values).Inner.OnConflict(conflictTarget);
        return (ExtUpsertBuilder)values;
    }

    public static PostgreSqlQuery DoUpdateSet(this IExtConflictAction action, params DbColumn[] updateColumns) =>
        new(((ExtUpsertBuilder)action).Inner.DoUpdateSet(updateColumns));

    public static PostgreSqlQuery DoNothing(this IExtConflictAction action) =>
        new(((ExtUpsertBuilder)action).Inner.DoNothing());
}
