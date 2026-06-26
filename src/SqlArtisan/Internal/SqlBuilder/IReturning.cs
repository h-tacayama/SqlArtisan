namespace SqlArtisan.Internal;

/// <summary>
/// The ability to add a <c>RETURNING</c> clause that reads back columns from the affected rows (PostgreSQL/SQLite/Oracle; SQL Server uses <c>OUTPUT</c>).
/// </summary>
public interface IReturning : ISqlBuilder
{
    /// <summary>
    /// Appends <c>RETURNING expr, ...</c> to read values from the inserted, updated, or deleted rows.
    /// </summary>
    /// <param name="expressions">The columns or expressions to return; use plain (unaliased) expressions, then chain <see cref="IReturningBuilder.Into(string[])"/> to bind them to output variables.</param>
    /// <returns>The builder positioned to chain <c>Into(...)</c> or build.</returns>
    /// <exception cref="ArgumentException">No expressions were supplied, or an expression carries an <c>.As(...)</c> alias (aliases are not allowed here — use <c>Into</c> for output variables).</exception>
    IReturningBuilder Returning(params object[] expressions);
}
