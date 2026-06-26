using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>GREATEST(a, b, ...)</c> function returning the largest of its
    /// arguments.
    /// </summary>
    /// <param name="expressions">The values to compare.</param>
    /// <returns>The <c>GREATEST</c> function expression.</returns>
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));

    /// <summary>
    /// A parenthesized grouping element for <c>GroupingSets(...)</c>,
    /// <c>Rollup(...)</c>, or <c>Cube(...)</c>. Rendered as <c>(a, b)</c> for two or
    /// more columns and as the bare column for a single column. Call with no
    /// columns — <c>Group()</c> — for the empty set <c>()</c> that produces the
    /// grand total.
    /// </summary>
    /// <param name="columns">The columns of the grouping element; none for the
    /// empty set <c>()</c>.</param>
    /// <returns>A <see cref="GroupingSet"/> rendering the parenthesized element.</returns>
    public static GroupingSet Group(params object[] columns) =>
        new(Resolve(columns));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite), using
    /// each dialect's default comma separator.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <returns>A <see cref="GroupConcatFunction"/> emitting <c>GROUP_CONCAT(expr)</c>.</returns>
    /// <remarks>A MySQL and SQLite aggregate; on other dialects use
    /// <see cref="Listagg(object, object)"/> (Oracle).</remarks>
    public static GroupConcatFunction GroupConcat(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr, separator)</c> string aggregate with SQLite's
    /// positional separator argument.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separator">The positional separator placed between values.</param>
    /// <returns>A <see cref="GroupConcatFunction"/> emitting
    /// <c>GROUP_CONCAT(expr, separator)</c>.</returns>
    /// <remarks>SQLite's positional separator form. For MySQL's <c>SEPARATOR</c>
    /// keyword form, pass <c>Sql.Separator(...)</c> instead.</remarks>
    public static GroupConcatFunction GroupConcat(object expr, object separator) =>
        new(Resolve(expr), positionalSeparator: Resolve(separator));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ...)</c> string aggregate with
    /// inline ordering and the default comma separator.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <returns>A <see cref="GroupConcatFunction"/> emitting
    /// <c>GROUP_CONCAT(expr ORDER BY ...)</c>.</returns>
    /// <remarks>A MySQL aggregate; on other dialects use
    /// <see cref="Listagg(object, object)"/> (Oracle).</remarks>
    public static GroupConcatFunction GroupConcat(object expr, OrderByClause orderByClause) =>
        new(Resolve(expr), orderByClause: orderByClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr SEPARATOR separator)</c> string aggregate,
    /// where <paramref name="separatorClause"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    /// <returns>A <see cref="GroupConcatFunction"/> emitting
    /// <c>GROUP_CONCAT(expr SEPARATOR separator)</c>.</returns>
    /// <remarks>A MySQL aggregate; on other dialects use
    /// <see cref="Listagg(object, object)"/> (Oracle).</remarks>
    public static GroupConcatFunction GroupConcat(object expr, SeparatorClause separatorClause) =>
        new(Resolve(expr), separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR separator)</c> string
    /// aggregate with inline ordering and an explicit
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    /// <returns>A <see cref="GroupConcatFunction"/> emitting
    /// <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR separator)</c>.</returns>
    /// <remarks>A MySQL aggregate; on other dialects use
    /// <see cref="Listagg(object, object)"/> (Oracle).</remarks>
    public static GroupConcatFunction GroupConcat(
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), orderByClause: orderByClause, separatorClause: separatorClause);

    /// <inheritdoc cref="GroupConcat(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    public static GroupConcatFunction GroupConcat(DistinctKeyword distinct, object expr) =>
        new(Resolve(expr), distinct: distinct);

    /// <inheritdoc cref="GroupConcat(object, OrderByClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause) =>
        new(Resolve(expr), distinct: distinct, orderByClause: orderByClause);

    /// <inheritdoc cref="GroupConcat(object, SeparatorClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), distinct: distinct, separatorClause: separatorClause);

    /// <inheritdoc cref="GroupConcat(object, OrderByClause, SeparatorClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), distinct: distinct, orderByClause: orderByClause, separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUPING SETS(...)</c> GROUP BY grouping extension, built from one or
    /// more <c>Group(...)</c> sets and emitted as <c>GROUPING SETS((a, b), c, ())</c>.
    /// </summary>
    /// <param name="set">The first grouping set, built with <c>Group(...)</c>.</param>
    /// <param name="sets">Any further grouping sets.</param>
    /// <returns>A <see cref="GroupingSetsGrouping"/> emitting
    /// <c>GROUPING SETS(...)</c>.</returns>
    /// <remarks>PostgreSQL, Oracle, and SQL Server support it; MySQL and SQLite do
    /// not, where it is emitted as written for the database to reject.</remarks>
    public static GroupingSetsGrouping GroupingSets(
        GroupingSet set,
        params GroupingSet[] sets) => new(set, sets);
}
