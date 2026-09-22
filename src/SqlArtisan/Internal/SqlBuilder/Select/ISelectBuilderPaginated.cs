namespace SqlArtisan.Internal;

/// <summary>
/// The state after a completed row-limiting chain (<c>OFFSET</c>, <c>FETCH FIRST</c>, or
/// <c>FETCH NEXT</c>): lock the selected rows with <c>FOR UPDATE</c>, build, or embed the query as
/// a subquery.
/// </summary>
public interface ISelectBuilderPaginated : ISqlBuilder, IForUpdate, ISubquery
{
}
