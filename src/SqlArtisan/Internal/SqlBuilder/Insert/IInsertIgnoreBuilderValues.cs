namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>VALUES</c> row on an <c>INSERT IGNORE</c>: append more rows, add <c>RETURNING</c>, or build. No upsert clause — <c>INSERT IGNORE</c> already resolves duplicate-key conflicts.
/// </summary>
public interface IInsertIgnoreBuilderValues : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends another row to the <c>VALUES</c> clause, producing a multi-row
    /// insert (<c>VALUES (...), (...)</c>).
    /// </summary>
    /// <param name="values">The row values, one per column; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(params object[] values);
}
