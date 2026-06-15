namespace SqlArtisan.Internal;

/// <summary>
/// UPSERT entry points available once an INSERT has its values. The methods are
/// per-dialect by design: <c>OnConflict</c> targets PostgreSQL/SQLite,
/// <c>OnDuplicateKeyUpdate</c> targets MySQL. The SQL you pick is the SQL that runs.
/// </summary>
public interface IInsertBuilderUpsertable
{
    /// <summary>
    /// PostgreSQL/SQLite <c>ON CONFLICT [(target)]</c>. Pass the conflict-target
    /// columns, or none for the implicit (any unique violation) form.
    /// </summary>
    IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget);

    /// <summary>
    /// MySQL <c>ON DUPLICATE KEY UPDATE</c>. The proposed row is exposed via the
    /// 8.0.19+ row alias (<c>AS new</c>); reference it with <see cref="ExcludedColumn"/>.
    /// </summary>
    IInsertBuilderUpsert OnDuplicateKeyUpdate(params EqualityBasedCondition[] assignments);
}
