namespace SqlArtisan.Internal;

/// <summary>
/// The row-limiting clauses that can follow a query. The forms are per-dialect: <c>LIMIT</c>/<c>OFFSET</c> (MySQL/PostgreSQL/SQLite) versus <c>OFFSET ... ROWS</c> / <c>FETCH ... ROWS ONLY</c> (Oracle/PostgreSQL/SQL Server).
/// </summary>
public interface IPagination
{
    /// <summary>
    /// Appends <c>FETCH FIRST n ROWS ONLY</c> with no offset.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The terminal builder; build, or embed as a subquery.</returns>
    /// <remarks>Standalone on Oracle and PostgreSQL; SQL Server requires an
    /// <c>OFFSET</c> — use <see cref="OffsetRows(int)"/> then
    /// <see cref="IOffsetFetchBuilder.FetchNext(int)"/> there.</remarks>
    ISelectBuilderPaginated FetchFirst(int count);

    /// <summary>
    /// Appends <c>LIMIT n</c>.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The builder positioned to optionally add an <c>OFFSET</c>, or build.</returns>
    /// <remarks>MySQL, PostgreSQL, and SQLite syntax; on Oracle use
    /// <see cref="FetchFirst(int)"/>, on SQL Server <see cref="OffsetRows(int)"/>
    /// then <see cref="IOffsetFetchBuilder.FetchNext(int)"/>.</remarks>
    ILimitOffsetBuilder Limit(int count);

    /// <summary>
    /// Appends <c>OFFSET m</c>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The terminal builder; build, or embed as a subquery.</returns>
    /// <remarks>MySQL, PostgreSQL, and SQLite — standalone only on PostgreSQL,
    /// while MySQL and SQLite accept it only with <see cref="Limit(int)"/>; on
    /// Oracle and SQL Server use <see cref="OffsetRows(int)"/>.</remarks>
    ISelectBuilderPaginated Offset(int start);

    /// <summary>
    /// Appends <c>OFFSET m ROWS</c>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The builder positioned to optionally add a <c>FETCH NEXT n ROWS ONLY</c>, or build.</returns>
    /// <remarks>Oracle, PostgreSQL, and SQL Server syntax; on MySQL and SQLite
    /// use <see cref="Offset(int)"/>.</remarks>
    IOffsetFetchBuilder OffsetRows(int start);
}
