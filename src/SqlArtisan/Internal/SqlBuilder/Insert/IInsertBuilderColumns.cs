namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table (col, ...)</c>: supply rows with <c>Values(...)</c> or feed them from a <c>SELECT</c> (or a <c>WITH</c> CTE).
/// </summary>
public interface IInsertBuilderColumns : ISqlBuilder, ISelectBuilder, IWithBuilder
{
    /// <summary>
    /// Appends a <c>VALUES (...)</c> row matching the declared column list.
    /// </summary>
    /// <param name="values">The row values, one per listed column; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(params object[] values);

    /// <summary>
    /// Appends one <c>VALUES (...)</c> row per element of <paramref name="rows"/>, each matching the declared column list — the collection-driven multi-row insert, without a per-row <c>Values(...)</c> call.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(IEnumerable<object[]> rows);

    /// <summary>
    /// Appends one <c>VALUES (...)</c> row per element of the <paramref name="rows"/> array — the array-typed sibling of the <see cref="IEnumerable{T}"/> overload, so a jagged <c>object[][]</c> (e.g. a <c>.Select(...).ToArray()</c>) is not ambiguous with the <c>params object[]</c> form.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(object[][] rows);
}
