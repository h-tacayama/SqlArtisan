namespace SqlArtisan.Internal;

/// <summary>
/// The terminal state after a completed row-limiting chain (<c>OFFSET</c>, <c>FETCH FIRST</c>, or <c>FETCH NEXT</c>): build, or embed the query as a subquery.
/// </summary>
public interface ISelectBuilderPaginated : ISqlBuilder, ISubquery
{
}
