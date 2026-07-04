namespace SqlArtisan.Internal;

/// <summary>
/// The row-limiting clauses that can follow a query. The forms are per-dialect: <c>LIMIT</c>/<c>OFFSET</c> (PostgreSQL/MySQL/SQLite) versus <c>OFFSET ... ROWS</c> / <c>FETCH ... ROWS ONLY</c> (Oracle 12c+/SQL Server 2012+).
/// </summary>
public interface IPagination
{
    /// <summary>
    /// Appends <c>LIMIT n</c>. Dialect-specific (PostgreSQL / MySQL / SQLite).
    /// For Oracle 12c+ / SQL Server 2012+ use <see cref="FetchFirst(int)"/>.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The builder positioned to optionally add an <c>OFFSET</c>, or build.</returns>
    ILimitOffsetBuilder Limit(int count);

    /// <summary>
    /// Appends <c>OFFSET m</c>. As a standalone clause this is valid on PostgreSQL;
    /// MySQL and SQLite require <c>OFFSET</c> to be combined with <see cref="Limit(int)"/>
    /// (<c>LIMIT n OFFSET m</c>). For Oracle 12c+ / SQL Server 2012+ use
    /// <see cref="OffsetRows(int)"/>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The terminal builder; call <see cref="ISqlBuilder.Build()"/>.</returns>
    ISqlBuilder Offset(int start);

    /// <summary>
    /// Appends <c>OFFSET m ROWS</c>. Dialect-specific (Oracle 12c+ / PostgreSQL /
    /// SQL Server 2012+). For MySQL / SQLite use <see cref="Offset(int)"/>.
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The builder positioned to optionally add a <c>FETCH NEXT n ROWS ONLY</c>, or build.</returns>
    IOffsetFetchBuilder OffsetRows(int start);

    /// <summary>
    /// Appends <c>FETCH FIRST n ROWS ONLY</c> with no offset. Valid standalone on
    /// Oracle 12c+ (and PostgreSQL). SQL Server requires an <c>OFFSET</c>, so there
    /// use <see cref="OffsetRows(int)"/> followed by
    /// <see cref="IOffsetFetchBuilder.FetchNext(int)"/>. For PostgreSQL / MySQL /
    /// SQLite, <see cref="Limit(int)"/> is the more common form.
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The terminal builder; call <see cref="ISqlBuilder.Build()"/>.</returns>
    ISqlBuilder FetchFirst(int count);
}
