namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>HAVING</c>: order, paginate, or build.
/// </summary>
public interface ISelectBuilderHaving : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
