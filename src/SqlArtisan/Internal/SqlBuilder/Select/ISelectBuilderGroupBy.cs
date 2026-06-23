namespace SqlArtisan.Internal;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    ISelectBuilderHaving Having(SqlCondition condition);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    /// <summary>
    /// Appends MySQL's <c>WITH ROLLUP</c> suffix to the <c>GROUP BY</c> clause
    /// (<c>GROUP BY a, b WITH ROLLUP</c>). This is MySQL's grouping syntax; on other
    /// dialects use the standard <c>Sql.Rollup(...)</c> function form instead.
    /// Returns <see cref="ISelectBuilderWithRollup"/>, which does not offer
    /// <c>WithRollup()</c>, so the suffix cannot be applied twice.
    /// </summary>
    ISelectBuilderWithRollup WithRollup();
}
