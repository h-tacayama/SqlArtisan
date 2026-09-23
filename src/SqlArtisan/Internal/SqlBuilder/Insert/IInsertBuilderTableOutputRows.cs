namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table OUTPUT ...</c> (SQL Server), once the optional
/// <c>INTO</c> redirect is settled: supply the rows positionally.
/// </summary>
public interface IInsertBuilderTableOutputRows
{
    /// <summary>
    /// Appends a positional <c>VALUES (...)</c> row for the table's columns in declaration order.
    /// </summary>
    /// <param name="values">The row values, one per column; must be non-empty, and literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, or build — <c>RETURNING</c> and the upsert clauses pair with no <c>OUTPUT</c>, so neither is reachable from here.</returns>
    IInsertBuilderValues Values(params object[] values);

    /// <summary>
    /// Appends one positional <c>VALUES (...)</c> row per element of <paramref name="rows"/> — the collection-driven multi-row insert, without a per-row <c>Values(...)</c> call.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, or build — <c>RETURNING</c> and the upsert clauses pair with no <c>OUTPUT</c>, so neither is reachable from here.</returns>
    IInsertBuilderValues Values(IEnumerable<object[]> rows);

    /// <summary>
    /// Appends one positional <c>VALUES (...)</c> row per element of the
    /// <paramref name="rows"/> array — the array-typed sibling of the
    /// <see cref="IEnumerable{T}"/> overload, keeping a jagged array off the <c>params</c> form.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, or build — <c>RETURNING</c> and the upsert clauses pair with no <c>OUTPUT</c>, so neither is reachable from here.</returns>
    IInsertBuilderValues Values(object[][] rows);
}
