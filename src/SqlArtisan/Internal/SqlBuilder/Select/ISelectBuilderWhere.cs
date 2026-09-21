namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>WHERE</c>: group, order, paginate, lock, build, or continue
/// with a set operator.
/// </summary>
public interface ISelectBuilderWhere : ISqlBuilder, IForUpdate, IPagination, ISetOperator, ISubquery
{
    /// <inheritdoc cref="ISelectBuilderFrom.GroupBy(object[])"/>
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
