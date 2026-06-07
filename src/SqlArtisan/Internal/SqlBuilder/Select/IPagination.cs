namespace SqlArtisan.Internal;

public interface IPagination
{
    /// <summary>
    /// Appends <c>LIMIT n</c>. Dialect-specific (PostgreSQL / MySQL / SQLite).
    /// For Oracle 12c+ / SQL Server 2012+ use <see cref="FetchFirst(int)"/>.
    /// </summary>
    ILimitOffsetBuilder Limit(int count);

    /// <summary>
    /// Appends <c>OFFSET m</c>. Dialect-specific (PostgreSQL / MySQL / SQLite).
    /// For Oracle 12c+ / SQL Server 2012+ use <see cref="OffsetRows(int)"/>.
    /// </summary>
    ISqlBuilder Offset(int start);

    /// <summary>
    /// Appends <c>OFFSET m ROWS</c>. Dialect-specific (Oracle 12c+ / SQL Server 2012+).
    /// For PostgreSQL / MySQL / SQLite use <see cref="Offset(int)"/>.
    /// </summary>
    IOffsetFetchBuilder OffsetRows(int start);

    /// <summary>
    /// Appends <c>FETCH FIRST n ROWS ONLY</c>. Dialect-specific (Oracle 12c+ /
    /// SQL Server 2012+). For PostgreSQL / MySQL / SQLite use <see cref="Limit(int)"/>.
    /// </summary>
    ISqlBuilder FetchFirst(int count);
}
