namespace SqlArtisan.Internal;

/// <summary>The state after <c>OFFSET m ROWS</c>: optionally add a <c>FETCH NEXT n ROWS ONLY</c> (Oracle 12c+/SQL Server 2012+), or build.</summary>
public interface IOffsetFetchBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>FETCH NEXT n ROWS ONLY</c> after <c>OFFSET m ROWS</c>.
    /// Dialect-specific (Oracle 12c+ / SQL Server 2012+).
    /// </summary>
    /// <param name="count">The maximum number of rows to return.</param>
    /// <returns>The terminal builder; call <see cref="ISqlBuilder.Build()"/>.</returns>
    ISqlBuilder FetchNext(int count);
}
