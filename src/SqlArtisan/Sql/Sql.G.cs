using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));

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
    /// The <c>GROUP_CONCAT(expr SEPARATOR separator)</c> string aggregate
    /// (MySQL), where <paramref name="separator"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, SeparatorClause separator) =>
        new(Resolve(expr), separatorClause: separator);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ...)</c> string aggregate (MySQL) with
    /// inline ordering and the default comma separator. The <c>ORDER BY</c> sits
    /// inside the call, so it is passed as an <c>Sql.OrderBy(...)</c> argument.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, OrderByClause orderBy) =>
        new(Resolve(expr), orderBy: orderBy);

    /// <summary>
    /// The <c>GROUP_CONCAT(expr ORDER BY ... SEPARATOR separator)</c> string
    /// aggregate (MySQL) with inline ordering and an explicit
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        object expr,
        OrderByClause orderBy,
        SeparatorClause separator) =>
        new(Resolve(expr), orderBy: orderBy, separatorClause: separator);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr)</c> string aggregate (MySQL, or SQLite
    /// in this single-argument form).
    /// </summary>
    public static GroupConcatFunction GroupConcat(DistinctKeyword distinct, object expr) =>
        new(Resolve(expr), distinct: distinct);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr SEPARATOR separator)</c> string
    /// aggregate (MySQL), where <paramref name="separator"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        SeparatorClause separator) =>
        new(Resolve(expr), distinct: distinct, separatorClause: separator);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr ORDER BY ...)</c> string aggregate
    /// (MySQL) with inline ordering and the default comma separator.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderBy) =>
        new(Resolve(expr), distinct: distinct, orderBy: orderBy);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr ORDER BY ... SEPARATOR separator)</c>
    /// string aggregate (MySQL) with inline ordering and an explicit
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        OrderByClause orderBy,
        SeparatorClause separator) =>
        new(Resolve(expr), distinct: distinct, orderBy: orderBy, separatorClause: separator);

    /// <summary>
    /// Wraps a <c>GROUP_CONCAT</c> separator in MySQL's <c>SEPARATOR</c> keyword
    /// form, distinguishing it from SQLite's positional separator argument.
    /// MySQL requires a string literal here, so <paramref name="separator"/> is
    /// emitted inline as an escaped literal rather than a bind parameter.
    /// </summary>
    public static SeparatorClause Separator(string separator) =>
        new(separator);
}
