using SqlArtisan.Internal;

namespace SqlArtisan.Databases.MySql;

// MySQL mirror of the PostgreSQL UPSERT surface: same delegation pattern, but the
// only reachable conflict transition is ON DUPLICATE KEY UPDATE (no ON CONFLICT).
// Note the state machine had to be re-authored for MySQL even though it shares
// the InsertBuilder — the wrappers cannot be reused across namespaces because
// their return types are DBMS-specific. That non-reuse is the cost being measured.
public static partial class Sql
{
    public static MySqlInsertColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new(global::SqlArtisan.Sql.InsertInto(table, columns));
}

public sealed class MySqlInsertColumns
{
    private readonly IInsertBuilderColumns _inner;

    internal MySqlInsertColumns(IInsertBuilderColumns inner) => _inner = inner;

    public MySqlInsertValues Values(params object[] values) =>
        new(_inner.Values(values));
}

public sealed class MySqlInsertValues
{
    private readonly IInsertBuilderValues _inner;

    internal MySqlInsertValues(IInsertBuilderValues inner) => _inner = inner;

    // No OnConflict here: MySQL's only UPSERT path is ON DUPLICATE KEY UPDATE.
    public MySqlQuery OnDuplicateKeyUpdate(params DbColumn[] updateColumns) =>
        new(_inner.OnDuplicateKeyUpdate(updateColumns));

    public MySqlQuery Build() => new(_inner);
}
