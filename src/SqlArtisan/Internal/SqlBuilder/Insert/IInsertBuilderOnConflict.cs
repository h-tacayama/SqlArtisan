namespace SqlArtisan.Internal;

/// <summary>
/// The action that follows <c>ON CONFLICT</c> (PostgreSQL/SQLite): either skip
/// the row (<c>DO NOTHING</c>) or update it (<c>DO UPDATE SET ...</c>).
/// </summary>
public interface IInsertBuilderOnConflict
{
    /// <summary>Appends <c>DO NOTHING</c>, silently skipping the row on a conflict.</summary>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IReturning DoNothing();

    /// <summary>Appends <c>DO UPDATE SET ...</c>, updating the conflicting row; reference the proposed row with <see cref="ExcludedColumn"/> (PostgreSQL/SQLite <c>excluded.col</c>).</summary>
    /// <param name="assignments">The <c>column == value</c> updates to apply to the existing row.</param>
    /// <returns>The builder positioned to add a <c>WHERE</c> filter, <c>RETURNING</c>, or build.</returns>
    IInsertBuilderDoUpdateSet DoUpdateSet(params EqualityBasedCondition[] assignments);
}
