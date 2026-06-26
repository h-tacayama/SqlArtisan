namespace SqlArtisan.Internal;

/// <summary>
/// The capability to write an UPSERT clause on an INSERT. The methods are
/// per-dialect: <c>OnConflict</c> targets PostgreSQL/SQLite,
/// <c>OnDuplicateKeyUpdate</c> targets MySQL.
/// </summary>
public interface IUpsert
{
    /// <summary>
    /// PostgreSQL/SQLite <c>ON CONFLICT [(target)]</c>. Pass the conflict-target
    /// columns, or none for the implicit (any unique violation) form.
    /// </summary>
    /// <param name="conflictTarget">The columns naming the unique constraint to test, or none for the implicit any-unique-violation form.</param>
    /// <returns>The builder positioned to supply the conflict action (<c>DO NOTHING</c> or <c>DO UPDATE SET</c>).</returns>
    IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget);

    /// <summary>
    /// MySQL <c>ON DUPLICATE KEY UPDATE</c>. The proposed row is exposed via the
    /// 8.0.19+ row alias (<c>AS new</c>); reference it with <see cref="ExcludedColumn"/>.
    /// </summary>
    /// <param name="assignments">The column assignments to apply when a duplicate-key conflict occurs.</param>
    /// <returns>The builder positioned to add <c>RETURNING</c> or build.</returns>
    IReturning OnDuplicateKeyUpdate(params EqualityBasedCondition[] assignments);
}
