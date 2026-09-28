namespace SqlArtisan.Internal;

/// <summary>
/// The row-limiting clauses that can follow a query. The forms are per-dialect:
/// <c>LIMIT</c>/<c>OFFSET</c> (MySQL/PostgreSQL/SQLite) versus <c>OFFSET ... ROWS</c> / <c>FETCH
/// ... ROWS ONLY</c> (Oracle/PostgreSQL/SQL Server).
/// </summary>
public interface IPagination
{
    /// <summary>
    /// Appends <c>FETCH FIRST n ROWS ONLY</c> with no offset.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The builder positioned after <c>FETCH FIRST</c>.</returns>
    /// <remarks>Standalone on Oracle and PostgreSQL; SQL Server requires an
    /// <c>OFFSET</c> — use <see cref="OffsetRows(int)"/> then
    /// <see cref="ISelectBuilderOffsetFetch.FetchNext(int)"/> there.</remarks>
    ISelectBuilderPaginated FetchFirst(int count);

    /// <summary>
    /// Appends <c>LIMIT n</c>.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The builder positioned after <c>LIMIT</c>.</returns>
    /// <remarks>MySQL, PostgreSQL, and SQLite syntax; on Oracle use
    /// <see cref="FetchFirst(int)"/>, on SQL Server <see cref="OffsetRows(int)"/>
    /// then <see cref="ISelectBuilderOffsetFetch.FetchNext(int)"/>.</remarks>
    ISelectBuilderLimitOffset Limit(int count);

    /// <summary>
    /// Appends <c>OFFSET m</c>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The builder positioned after <c>OFFSET</c>.</returns>
    /// <remarks>MySQL, PostgreSQL, and SQLite syntax — standalone only on PostgreSQL; MySQL
    /// and SQLite take it only after <see cref="Limit(int)"/> (<c>Build(Dbms)</c> throws
    /// there without one). Oracle and SQL Server: <see cref="OffsetRows(int)"/>.</remarks>
    ISelectBuilderPaginated Offset(int start);

    /// <summary>
    /// Appends <c>OFFSET m ROWS</c>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The builder positioned after <c>OFFSET m ROWS</c>.</returns>
    /// <remarks>Oracle, PostgreSQL, and SQL Server syntax; on MySQL and SQLite
    /// use <see cref="Offset(int)"/>.</remarks>
    ISelectBuilderOffsetFetch OffsetRows(int start);
}
