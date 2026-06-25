namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after a <c>GROUP BY ... WITH ROLLUP</c> clause. It offers the
/// same continuations as the <c>GROUP BY</c> state (<c>HAVING</c>, <c>ORDER BY</c>,
/// pagination, set operators, terminal build) but not <c>WithRollup()</c> itself,
/// so the MySQL suffix cannot be applied twice. This is what
/// <see cref="ISelectBuilderGroupBy.WithRollup"/> returns.
/// </summary>
public interface ISelectBuilderWithRollup : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    /// <inheritdoc cref="ISelectBuilderGroupBy.Having(SqlCondition)"/>
    ISelectBuilderHaving Having(SqlCondition condition);

    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
