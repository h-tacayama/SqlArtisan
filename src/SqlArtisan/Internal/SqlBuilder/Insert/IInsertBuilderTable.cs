namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table</c> (no column list): supply the row by column assignments or positionally.
/// </summary>
public interface IInsertBuilderTable : ISqlBuilder
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
}
