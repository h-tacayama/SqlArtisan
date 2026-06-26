namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after a <c>GROUP BY ... WITH ROLLUP</c> clause: continue with
/// <c>HAVING</c>, <c>ORDER BY</c>, pagination, a set operator, or build.
/// </summary>
public interface ISelectBuilderWithRollup : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    /// <inheritdoc cref="ISelectBuilderGroupBy.Having(SqlCondition)"/>
    ISelectBuilderHaving Having(SqlCondition condition);

    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
