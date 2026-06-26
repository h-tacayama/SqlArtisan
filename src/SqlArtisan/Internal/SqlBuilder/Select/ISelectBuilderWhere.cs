namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>WHERE</c>: group, order, paginate, lock, or build.
/// </summary>
public interface ISelectBuilderWhere : ISqlBuilder, ISetOperator, IForUpdate, ISubquery, IPagination
{
    /// <inheritdoc cref="ISelectBuilderFrom.GroupBy(object[])"/>
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
