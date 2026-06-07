namespace SqlArtisan.Internal;

public interface IOffsetFetchBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>FETCH NEXT n ROWS ONLY</c> after <c>OFFSET m ROWS</c>.
    /// Dialect-specific (Oracle 12c+ / SQL Server 2012+).
    /// </summary>
    ISqlBuilder FetchNext(int count);
}
