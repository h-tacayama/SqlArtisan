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

    /// <summary>Appends <c>ON DUPLICATE KEY UPDATE assignment, ...</c>.</summary>
    /// <remarks>MySQL (8.0.19+): the proposed row is exposed through the
    /// <c>AS new</c> row alias; reference it with <see cref="ExcludedColumn"/>.</remarks>
    /// <param name="assignments">The column assignments to apply when a duplicate-key conflict occurs.</param>
    /// <returns>The builder positioned to build; MySQL has no <c>RETURNING</c>.</returns>
    ISqlBuilder OnDuplicateKeyUpdate(params EqualityCondition[] assignments);
}
