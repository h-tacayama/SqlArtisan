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
    /// </summary>
    ISelectBuilderGroupBy WithRollup();
}
