using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));

    /// <summary>
    /// A parenthesized grouping element for <c>GroupingSets(...)</c>,
    /// <c>Rollup(...)</c>, or <c>Cube(...)</c>. Rendered as <c>(a, b)</c> for two or
    /// more columns and as the bare column for a single column. Call with no
    /// columns — <c>Group()</c> — for the empty set <c>()</c> that produces the
    /// grand total.
    /// </summary>
    public static GroupingSet Group(params object[] columns) =>
        new(Resolve(columns));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite), using
    /// each dialect's default comma separator.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr, separator)</c> string aggregate with SQLite's
    /// positional separator argument. For MySQL's <c>SEPARATOR</c> keyword form,
    /// pass <c>Sql.Separator(...)</c> instead.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, object separator) =>
        new(Resolve(expr), positionalSeparator: Resolve(separator));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ...)</c> string aggregate (MySQL) with
    /// inline ordering and the default comma separator. The <c>ORDER BY</c> sits
    /// inside the call, so it is passed as an <c>Sql.OrderBy(...)</c> argument.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, OrderByClause orderByClause) =>
        new(Resolve(expr), orderByClause: orderByClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr SEPARATOR separator)</c> string aggregate
    /// (MySQL), where <paramref name="separatorClause"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, SeparatorClause separatorClause) =>
        new(Resolve(expr), separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR separator)</c> string
    /// aggregate (MySQL) with inline ordering and an explicit
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), orderByClause: orderByClause, separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr)</c> string aggregate (MySQL, or SQLite
    /// in this single-argument form).
    /// </summary>
    public static GroupConcatFunction GroupConcat(DistinctKeyword distinct, object expr) =>
        new(Resolve(expr), distinct: distinct);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr ORDER BY ...)</c> string aggregate
    /// (MySQL) with inline ordering and the default comma separator.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause) =>
        new(Resolve(expr), distinct: distinct, orderByClause: orderByClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr SEPARATOR separator)</c> string
    /// aggregate (MySQL), where <paramref name="separatorClause"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), distinct: distinct, separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr ORDER BY ... SEPARATOR separator)</c>
    /// string aggregate (MySQL) with inline ordering and an explicit
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new(Resolve(expr), distinct: distinct, orderByClause: orderByClause, separatorClause: separatorClause);

    /// <summary>
    /// The <c>GROUPING SETS(...)</c> GROUP BY grouping extension, built from one or
    /// more <c>Group(...)</c> sets and emitted as <c>GROUPING SETS((a, b), c, ())</c>
    /// on PostgreSQL / Oracle / SQL Server; MySQL and SQLite throw at build time.
    /// </summary>
    public static GroupingSetsGrouping GroupingSets(
        GroupingSet set,
        params GroupingSet[] sets) => new([set, .. sets]);
}
