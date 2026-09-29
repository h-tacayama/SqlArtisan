namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table</c> (no column list): supply the row by column assignments
/// or positionally. Not buildable until a row source is supplied.
/// </summary>
public interface IInsertBuilderTable
{
    /// <summary>
    /// Builds the row from <c>column == value</c> assignments, emitting the column list and one
    /// <c>VALUES</c> row from them (<c>INSERT INTO t (code, name) VALUES (:0, :1)</c>).
    /// </summary>
    /// <param name="assignments">The per-column assignments; each left side names a column and each right side its value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>RETURNING</c>, an upsert clause, or build.</returns>
    IInsertBuilderSet Set(params EqualityCondition[] assignments);

    /// <summary>
    /// Appends a positional <c>VALUES (...)</c> row for the table's columns in declaration order.
    /// </summary>
    /// <param name="values">The row values, one per column; must be non-empty, and literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(params object[] values);

    /// <summary>
    /// Appends one positional <c>VALUES (...)</c> row per element of <paramref name="rows"/> — the
    /// collection-driven multi-row insert, without a per-row <c>Values(...)</c> call.
    /// </summary>
    /// <param name="rows">The rows, each an array of values in column order; must be non-empty, and every row must be the same width.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite, and SQL Server — on Oracle
    /// version-bound when it emits more than one row: 21c rejects a multi-row
    /// <c>VALUES</c>, 23ai accepts it.</remarks>
    IInsertBuilderValues Values(IEnumerable<object[]> rows);

    /// <inheritdoc cref="Values(IEnumerable{object[]})"/>
    IInsertBuilderValues Values(object[][] rows);
}
