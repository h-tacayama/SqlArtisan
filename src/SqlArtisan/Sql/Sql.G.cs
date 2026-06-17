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
        new(Resolve(expr), Resolve(separator));

    /// <summary>
    /// The <c>GROUP_CONCAT(expr SEPARATOR separator)</c> string aggregate
    /// (MySQL), where <paramref name="separator"/> is built with
    /// <c>Sql.Separator(...)</c>. Chain <c>.OrderBy(...)</c> to order the values.
    /// </summary>
    public static GroupConcatFunction GroupConcat(object expr, SeparatorClause separator) =>
        new(Resolve(expr), separator);

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr)</c> string aggregate (MySQL).
    /// </summary>
    public static GroupConcatFunction GroupConcat(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    /// <summary>
    /// The <c>GROUP_CONCAT(DISTINCT expr SEPARATOR separator)</c> string
    /// aggregate (MySQL), where <paramref name="separator"/> is built with
    /// <c>Sql.Separator(...)</c>.
    /// </summary>
    public static GroupConcatFunction GroupConcat(
        DistinctKeyword distinct,
        object expr,
        SeparatorClause separator) => new(distinct, Resolve(expr), separator);

    /// <summary>
    /// Wraps a <c>GROUP_CONCAT</c> separator in MySQL's <c>SEPARATOR</c> keyword
    /// form, distinguishing it from SQLite's positional separator argument.
    /// MySQL requires a string literal here, so <paramref name="separator"/> is
    /// emitted inline as an escaped literal rather than a bind parameter.
    /// </summary>
    public static SeparatorClause Separator(string separator) =>
        new(separator);
}
