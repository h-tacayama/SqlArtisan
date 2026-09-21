namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>HAVING</c>: order, paginate, build, or continue with a set operator.
/// </summary>
public interface ISelectBuilderHaving : ISqlBuilder, IPagination, ISetOperator, ISubquery
{
    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
