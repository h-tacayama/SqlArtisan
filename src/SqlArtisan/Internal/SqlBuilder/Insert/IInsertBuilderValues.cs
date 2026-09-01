namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>VALUES</c> row: append more rows, add <c>RETURNING</c> or an upsert clause, or build.
/// </summary>
public interface IInsertBuilderValues : ISqlBuilder, IReturning, IUpsert
{
    /// <summary>
    /// Appends another row to the <c>VALUES</c> clause, producing a multi-row
    /// insert (<c>VALUES (...), (...)</c>).
    /// </summary>
    /// <param name="values">The row values, one per column; must be non-empty, and literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite, and SQL Server — on Oracle
    /// version-bound: 21c rejects it, 23ai accepts it (both live-verified).</remarks>
    IInsertBuilderValues Values(params object[] values);
}
