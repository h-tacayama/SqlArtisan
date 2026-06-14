using SqlArtisan.Internal;

namespace SqlArtisan.Databases.PostgreSql;

// ─────────────────────────────────────────────────────────────────────────────
// Approach B at the CLAUSE level: the PostgreSQL namespace exposes only ON
// CONFLICT (not ON DUPLICATE KEY UPDATE, not MERGE). Unlike the scalar facades —
// which the generator emits from the catalog — a fluent clause API cannot be a
// flat list of methods: each intermediate state (after Values, after OnConflict)
// is its own type, and every per-DBMS namespace must mirror that whole state
// machine. These wrappers delegate to the shared InsertBuilder; their only job
// is to narrow which transitions are reachable. The boilerplate below — three
// wrapper types for one statement shape, one DBMS — IS the maintenance-cost
// measurement (see the spike README write-up).
// ─────────────────────────────────────────────────────────────────────────────
public static partial class Sql
{
    public static PostgreSqlInsertColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new(global::SqlArtisan.Sql.InsertInto(table, columns));
}

public sealed class PostgreSqlInsertColumns
{
    private readonly IInsertBuilderColumns _inner;

    internal PostgreSqlInsertColumns(IInsertBuilderColumns inner) => _inner = inner;

    public PostgreSqlInsertValues Values(params object[] values) =>
        new(_inner.Values(values));
}

public sealed class PostgreSqlInsertValues
{
    private readonly IInsertBuilderValues _inner;

    internal PostgreSqlInsertValues(IInsertBuilderValues inner) => _inner = inner;

    public PostgreSqlOnConflict OnConflict(params DbColumn[] conflictTarget) =>
        new(_inner.OnConflict(conflictTarget));

    // No OnDuplicateKeyUpdate here: that is the namespace filtering in action.
    public PostgreSqlQuery Build() => new(_inner);
}

public sealed class PostgreSqlOnConflict
{
    private readonly IOnConflictBuilder _inner;

    internal PostgreSqlOnConflict(IOnConflictBuilder inner) => _inner = inner;

    public PostgreSqlQuery DoNothing() => new(_inner.DoNothing());

    public PostgreSqlQuery DoUpdateSet(params DbColumn[] updateColumns) =>
        new(_inner.DoUpdateSet(updateColumns));
}
