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
    /// <remarks>Not supported by SQLite — its multi-argument
    /// <c>MAX(a, b, ...)</c> is the equivalent; SQL Server (2022+).</remarks>
    public static SqlExpression Greatest(params object[] expressions) =>
        new GreatestFunction(Resolve(expressions, nameof(expressions)));

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
    /// <exception cref="ArgumentException">An element is a value rather than an expression or a
    /// subquery, which would bind as a constant that groups nothing.</exception>
    public static GroupingSet Group(params object[] columns) =>
        new(ResolveKeys(
            columns,
            nameof(columns),
            "GroupByItem",
            $"{Keywords.Group} {Keywords.By}"));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr)</c> string aggregate (MySQL and SQLite), using
    /// each dialect's default comma separator.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <returns>An expression emitting <c>GROUP_CONCAT(expr)</c>.</returns>
    /// <remarks>A MySQL and SQLite aggregate; on other dialects use
    /// <see cref="Listagg(object, object)"/> (Oracle).</remarks>
    public static SqlExpression GroupConcat(object expr) =>
        new GroupConcatFunction(Resolve(expr, nameof(expr)));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr, separator)</c> string aggregate with SQLite's
    /// positional separator argument.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separator">The positional separator placed between values.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(expr, separator)</c>.</returns>
    /// <remarks>SQLite's positional separator form. On MySQL the same call runs
    /// but the second argument concatenates as another value per row and the
    /// separator stays the default comma — spell MySQL's separator with
    /// <c>Sql.Separator(...)</c> instead.</remarks>
    public static SqlExpression GroupConcat(object expr, object separator) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            positionalSeparator: Resolve(separator, nameof(separator)));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ...)</c> string aggregate with
    /// inline ordering and the default comma separator.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(expr ORDER BY ...)</c>.</returns>
    /// <remarks>MySQL and SQLite syntax; on Oracle use
    /// <see cref="Listagg(object, object)"/>.</remarks>
    public static SqlExpression GroupConcat(object expr, OrderByClause orderByClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            orderByClause: NullGuard.ThrowIfNull(orderByClause, nameof(orderByClause)));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr SEPARATOR separator)</c> string aggregate,
    /// where <paramref name="separatorClause"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(expr SEPARATOR separator)</c>.</returns>
    /// <remarks>MySQL syntax; on Oracle use
    /// <see cref="Listagg(object, object)"/>.</remarks>
    public static SqlExpression GroupConcat(object expr, SeparatorClause separatorClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            separatorClause: NullGuard.ThrowIfNull(separatorClause, nameof(separatorClause)));

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
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR separator)</c>.</returns>
    /// <inheritdoc cref="GroupConcat(object, SeparatorClause)" path="/remarks"/>
    public static SqlExpression GroupConcat(
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            orderByClause: NullGuard.ThrowIfNull(orderByClause, nameof(orderByClause)),
            separatorClause: NullGuard.ThrowIfNull(separatorClause, nameof(separatorClause)));

    /// <inheritdoc cref="GroupConcat(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(DISTINCT expr)</c>.</returns>
    public static SqlExpression GroupConcat(DistinctKeyword distinct, object expr) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            distinct: NullGuard.ThrowIfNull(distinct, nameof(distinct)));

    /// <inheritdoc cref="GroupConcat(object, OrderByClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(DISTINCT expr ORDER BY ...)</c>.</returns>
    public static SqlExpression GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            distinct: NullGuard.ThrowIfNull(distinct, nameof(distinct)),
            orderByClause: NullGuard.ThrowIfNull(orderByClause, nameof(orderByClause)));

    /// <inheritdoc cref="GroupConcat(object, SeparatorClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(DISTINCT expr SEPARATOR separator)</c>.</returns>
    public static SqlExpression GroupConcat(
        DistinctKeyword distinct,
        object expr,
        SeparatorClause separatorClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            distinct: NullGuard.ThrowIfNull(distinct, nameof(distinct)),
            separatorClause: NullGuard.ThrowIfNull(separatorClause, nameof(separatorClause)));

    /// <inheritdoc cref="GroupConcat(object, OrderByClause, SeparatorClause)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// aggregating only distinct values.</param>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="orderByClause">The inline ordering, built with
    /// <c>Sql.OrderBy(...)</c>.</param>
    /// <param name="separatorClause">The <c>SEPARATOR</c> clause, built with
    /// <c>Sql.Separator(...)</c>.</param>
    /// <returns>An expression emitting
    /// <c>GROUP_CONCAT(DISTINCT expr ORDER BY ... SEPARATOR separator)</c>.</returns>
    public static SqlExpression GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderByClause,
        SeparatorClause separatorClause) =>
        new GroupConcatFunction(
            Resolve(expr, nameof(expr)),
            distinct: NullGuard.ThrowIfNull(distinct, nameof(distinct)),
            orderByClause: NullGuard.ThrowIfNull(orderByClause, nameof(orderByClause)),
            separatorClause: NullGuard.ThrowIfNull(separatorClause, nameof(separatorClause)));

    /// <summary>
    /// The <c>GROUPING(<paramref name="expr"/>)</c> function: <c>1</c> when
    /// <paramref name="expr"/> is aggregated away in the current
    /// grouping-extension row (a subtotal), <c>0</c> otherwise — distinguishing a
    /// subtotal row from a genuine <c>NULL</c> data row.
    /// </summary>
    /// <param name="expr">The grouping-extension column to test.</param>
    /// <returns>An expression emitting <c>GROUPING(expr)</c>.</returns>
    /// <remarks>MySQL (8.0.1+), Oracle, PostgreSQL, and SQL Server; not SQLite,
    /// which has no grouping extensions.</remarks>
    public static SqlExpression Grouping(object expr) =>
        new GroupingFunction(Resolve(expr, nameof(expr)));

    /// <summary>
    /// The multi-column <c>GROUPING(<paramref name="expr1"/>, <paramref name="expr2"/>, ...)</c>
    /// bitmask function: each argument contributes a bit (1 when aggregated away),
    /// combined into a single integer.
    /// </summary>
    /// <param name="expr1">The first grouping-extension column.</param>
    /// <param name="expr2">The second grouping-extension column.</param>
    /// <param name="others">Any further grouping-extension columns.</param>
    /// <returns>An expression emitting <c>GROUPING(a, b, ...)</c>.</returns>
    /// <remarks>MySQL (8.0.1+) and PostgreSQL only; Oracle accepts only the single-argument
    /// form (use <see cref="GroupingId(object, object[])"/> there instead), and
    /// neither SQLite nor SQL Server support it.</remarks>
    public static SqlExpression Grouping(object expr1, object expr2, params object[] others) =>
        new GroupingFunction(ResolveVariadic(
            expr1,
            nameof(expr1),
            expr2,
            nameof(expr2),
            others,
            nameof(others)));

    /// <summary>
    /// The <c>GROUPING_ID(<paramref name="expr"/>, ...)</c> bitmask function: each
    /// argument contributes a bit (1 when aggregated away in the current
    /// grouping-extension row), combined into a single integer.
    /// </summary>
    /// <param name="expr">The first grouping-extension column.</param>
    /// <param name="others">Any further grouping-extension columns.</param>
    /// <returns>An expression emitting
    /// <c>GROUPING_ID(a, ...)</c>.</returns>
    /// <remarks>Oracle and SQL Server syntax; for MySQL/PostgreSQL's equivalent
    /// bitmask use <see cref="Grouping(object, object, object[])"/> instead.</remarks>
    public static SqlExpression GroupingId(object expr, params object[] others) =>
        new GroupingIdFunction(ResolveVariadic(expr, nameof(expr), others, nameof(others)));

    /// <summary>
    /// The <c>GROUPING SETS(...)</c> GROUP BY grouping extension, built from one or
    /// more <c>Group(...)</c> sets and emitted as <c>GROUPING SETS((a, b), c, ())</c>.
    /// </summary>
    /// <param name="set">The first grouping set, built with <c>Group(...)</c>.</param>
    /// <param name="sets">Any further grouping sets.</param>
    /// <returns>A grouping element emitting
    /// <c>GROUPING SETS(...)</c>.</returns>
    /// <remarks>PostgreSQL, Oracle, and SQL Server support it; MySQL and SQLite do
    /// not, where it is emitted as written for the database to reject.</remarks>
    public static SqlPart GroupingSets(GroupingSet set, params GroupingSet[] sets) =>
        new GroupingSetsGrouping(set, sets);
}
