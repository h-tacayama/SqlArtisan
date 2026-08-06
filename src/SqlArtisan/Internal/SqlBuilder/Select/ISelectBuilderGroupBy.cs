namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>GROUP BY</c>: filter groups with <c>HAVING</c>, order, paginate, build, or append <c>WITH ROLLUP</c> (MySQL and SQL Server).
/// </summary>
public interface ISelectBuilderGroupBy : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    /// <summary>
    /// Appends <c>HAVING condition</c> to filter on aggregated groups.
    /// </summary>
    /// <param name="condition">The group filter, typically over an aggregate; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned after <c>HAVING</c>, ready for <c>ORDER BY</c>, pagination, or build.</returns>
    ISelectBuilderHaving Having(SqlCondition condition);

    /// <inheritdoc cref="ISelectBuilderFrom.OrderBy(object[])"/>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    /// <summary>
    /// Appends the <c>WITH ROLLUP</c> suffix to the <c>GROUP BY</c> clause
    /// (<c>GROUP BY a, b WITH ROLLUP</c>).
    /// </summary>
    /// <remarks>
    /// MySQL's grouping syntax, which SQL Server accepts as a legacy form. On
    /// Oracle and PostgreSQL use the standard <c>Sql.Rollup(...)</c> function
    /// form; SQLite has neither.
    /// </remarks>
    ISelectBuilderWithRollup WithRollup();
}
