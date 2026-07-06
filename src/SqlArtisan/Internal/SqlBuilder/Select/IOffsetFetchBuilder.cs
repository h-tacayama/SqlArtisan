namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>OFFSET m ROWS</c>: optionally add a <c>FETCH NEXT n ROWS ONLY</c> (Oracle 12c+/SQL Server 2012+), build, or embed as a subquery.
/// </summary>
public interface IOffsetFetchBuilder : ISqlBuilder, ISubquery
{
    /// <summary>
    /// Appends <c>FETCH NEXT n ROWS ONLY</c> after <c>OFFSET m ROWS</c>.
    /// Dialect-specific (Oracle 12c+ / PostgreSQL / SQL Server 2012+).
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The terminal builder; build, or embed as a subquery.</returns>
    ISelectBuilderPaginated FetchNext(int count);
}
