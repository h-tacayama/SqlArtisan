namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table</c> (no column list): supply the row by column assignments or positionally. Not buildable until a row source is supplied.
/// </summary>
public interface IInsertBuilderTable
{
    /// <summary>
    /// Builds the row from <c>column == value</c> assignments, emitting the column list and one <c>VALUES</c> row from them (<c>INSERT INTO t (code, name) VALUES (:0, :1)</c>).
    /// </summary>
    /// <param name="assignments">The per-column assignments; each left side names a column and each right side its value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>RETURNING</c>, an upsert clause, or build.</returns>
    IInsertBuilderSet Set(params EqualityBasedCondition[] assignments);

    /// <summary>
    /// Appends a positional <c>VALUES (...)</c> row for the table's columns in declaration order.
    /// </summary>
    /// <param name="values">The row values, one per column; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(params object[] values);

    /// <summary>
    /// Appends one positional <c>VALUES (...)</c> row per element of <paramref name="rows"/> — the collection-driven multi-row insert, without a per-row <c>Values(...)</c> call.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(IEnumerable<object[]> rows);

    /// <summary>
    /// Appends one positional <c>VALUES (...)</c> row per element of the <paramref name="rows"/> array — the array-typed sibling of the <see cref="IEnumerable{T}"/> overload, so a jagged <c>object[][]</c> (e.g. a <c>.Select(...).ToArray()</c>) is not ambiguous with the <c>params object[]</c> form.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(object[][] rows);
}
