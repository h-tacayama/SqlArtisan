namespace SqlArtisan.Internal;

/// <summary>
/// The ability to add a <c>RETURNING</c> clause that reads back columns from the affected rows
/// (PostgreSQL/SQLite/Oracle; SQL Server uses <c>OUTPUT</c>).
/// </summary>
public interface IReturning : ISqlBuilder
{
    /// <summary>
    /// Appends <c>RETURNING expr, ...</c> to read values from the inserted, updated, or deleted
    /// rows.
    /// </summary>
    /// <param name="expressions">The columns or expressions to return; an alias renders as written, but <see cref="IReturningBuilder.Into"/>, which binds them to output parameters, takes unaliased expressions only.</param>
    /// <returns>The builder positioned to chain <c>Into(...)</c> or build.</returns>
    /// <exception cref="ArgumentException">No expressions were supplied.</exception>
    /// <remarks>Oracle, PostgreSQL, and SQLite (3.35+) syntax.</remarks>
    IReturningBuilder Returning(params object[] expressions);
}
