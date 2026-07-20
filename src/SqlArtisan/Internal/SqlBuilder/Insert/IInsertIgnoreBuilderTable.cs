namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table</c> (no column list): supply the row by column assignments or positionally. Not buildable until a row source is supplied.
/// </summary>
public interface IInsertIgnoreBuilderTable
{
    /// <summary>
    /// Builds the row from <c>column == value</c> assignments, emitting the column list and one <c>VALUES</c> row from them (<c>INSERT IGNORE INTO t (code, name) VALUES (:0, :1)</c>).
    /// </summary>
    /// <param name="assignments">The per-column assignments; each left side names a column and each right side its value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IInsertIgnoreBuilderSet Set(params EqualityBasedCondition[] assignments);

    /// <inheritdoc cref="IInsertBuilderTable.Values(object[])"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(params object[] values);

    /// <inheritdoc cref="IInsertBuilderTable.Values(IEnumerable{object[]})"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(IEnumerable<object[]> rows);

    /// <inheritdoc cref="IInsertBuilderTable.Values(object[][])"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(object[][] rows);
}
