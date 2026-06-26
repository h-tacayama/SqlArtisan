namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after <c>FROM</c>: join more tables, filter, group, order, paginate, or build.
/// </summary>
public interface ISelectBuilderFrom : ISqlBuilder, IJoinOperator, ISetOperator, IForUpdate, ISubquery, IPagination
{
    /// <summary>
    /// Appends <c>GROUP BY a, b, ...</c>.
    /// </summary>
    /// <param name="groupByItems">The grouping expressions, or grouping constructs such as <c>Sql.Rollup(...)</c>, <c>Sql.Cube(...)</c>, and <c>Sql.GroupingSets(...)</c>.</param>
    /// <returns>The builder positioned after <c>GROUP BY</c>, ready for <c>HAVING</c>, <c>ORDER BY</c>, or build.</returns>
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    /// <summary>
    /// Appends <c>ORDER BY a, b, ...</c>.
    /// </summary>
    /// <param name="orderByItems">The sort keys — columns or expressions, optionally wrapped with <c>Sql.Asc(...)</c> / <c>Sql.Desc(...)</c> or a null-ordering modifier.</param>
    /// <returns>The builder positioned after <c>ORDER BY</c>, ready for pagination, locking, or build.</returns>
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    /// <summary>
    /// Appends <c>WHERE condition</c>.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned after <c>WHERE</c>, ready for grouping, ordering, pagination, locking, or build.</returns>
    ISelectBuilderWhere Where(SqlCondition condition);
}
