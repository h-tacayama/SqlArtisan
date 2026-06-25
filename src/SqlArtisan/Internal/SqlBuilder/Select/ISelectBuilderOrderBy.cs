namespace SqlArtisan.Internal;

/// <summary>The builder state after <c>ORDER BY</c>: paginate, lock, or build.</summary>
public interface ISelectBuilderOrderBy : ISqlBuilder, IForUpdate, ISubquery, IPagination
{
}
