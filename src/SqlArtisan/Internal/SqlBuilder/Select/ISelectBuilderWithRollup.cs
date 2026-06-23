namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after a <c>GROUP BY</c> clause that may still take MySQL's
/// <c>WITH ROLLUP</c> suffix. <see cref="ISelectBuilderGroupBy"/> adds
/// <see cref="ISelectBuilderGroupBy.WithRollup"/> on top of this; calling it
/// returns this narrower surface so the suffix cannot be applied twice.
/// </summary>
public interface ISelectBuilderWithRollup : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    ISelectBuilderHaving Having(SqlCondition condition);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
