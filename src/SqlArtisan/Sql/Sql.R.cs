using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>RANK()</c> analytic function: the rank of the current row within its
    /// window partition, with gaps after ties. Complete it with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>A <c>RANK</c> analytic function expression.</returns>
    public static AnalyticRankFunction Rank() => new();

    /// <summary>
    /// The <c>ROW_NUMBER()</c> analytic function: a sequential number per row within
    /// its window partition. Complete it with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>A <c>ROW_NUMBER</c> analytic function expression.</returns>
    public static AnalyticRowNumberFunction RowNumber() => new();

    /// <summary>
    /// The <c>REGEXP_COUNT(source, pattern)</c> function: the number of times
    /// <paramref name="pattern"/> matches in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <returns>A <c>REGEXP_COUNT</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    /// <inheritdoc cref="RegexpCount(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    /// <inheritdoc cref="RegexpCount(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="options">Match modifiers, emitted as Oracle's flag literal (e.g. <c>'i'</c>).</param>
    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern,
        object position,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            options);

    /// <summary>
    /// The <c>REGEXP_LIKE(source, pattern)</c> predicate: true when
    /// <paramref name="source"/> matches the regular-expression
    /// <paramref name="pattern"/>.
    /// </summary>
    /// <param name="source">The string tested.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <returns>A <c>REGEXP_LIKE</c> condition.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static RegexpLikeCondition RegexpLike(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    /// <inheritdoc cref="RegexpLike(object, object)"/>
    /// <param name="source">The string tested.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="options">Match modifiers, emitted as Oracle's flag literal (e.g. <c>'i'</c>).</param>
    public static RegexpLikeCondition RegexpLike(
        object source,
        object pattern,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            options);

    /// <summary>
    /// The <c>REGEXP_REPLACE(source, pattern, replacement)</c> function: replaces
    /// each match of <paramref name="pattern"/> in <paramref name="source"/> with
    /// <paramref name="replacement"/>.
    /// </summary>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="replacement">The replacement text (may reference capture groups).</param>
    /// <returns>A <c>REGEXP_REPLACE</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement));

    /// <inheritdoc cref="RegexpReplace(object, object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="replacement">The replacement text (may reference capture groups).</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement),
            Resolve(position));

    /// <inheritdoc cref="RegexpReplace(object, object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="replacement">The replacement text (may reference capture groups).</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="occurrence">Which match to replace; <c>0</c> replaces all.</param>
    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement),
            Resolve(position),
            Resolve(occurrence));

    /// <inheritdoc cref="RegexpReplace(object, object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="replacement">The replacement text (may reference capture groups).</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="occurrence">Which match to replace; <c>0</c> replaces all.</param>
    /// <param name="options">Match modifiers, emitted as Oracle's flag literal (e.g. <c>'i'</c>).</param>
    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement,
        object position,
        object occurrence,
        RegexpOptions options) =>
        new(Resolve(source),
            Resolve(pattern),
            Resolve(replacement),
            Resolve(position),
            Resolve(occurrence),
            options);

    /// <summary>
    /// The <c>REPLACE(source, search, replacement)</c> function: replaces every
    /// literal occurrence of <paramref name="search"/> in <paramref name="source"/>
    /// with <paramref name="replacement"/>.
    /// </summary>
    /// <param name="source">The string searched.</param>
    /// <param name="search">The substring to find.</param>
    /// <param name="replacement">The replacement substring.</param>
    /// <returns>A <c>REPLACE</c> function expression.</returns>
    public static ReplaceFunction Replace(
        object source,
        object search,
        object replacement) => new(
            Resolve(source),
            Resolve(search),
            Resolve(replacement));

    /// <summary>
    /// The <c>ROLLUP(...)</c> GROUP BY grouping extension. Each element is an
    /// ordinary column or a <c>Sql.Group(...)</c> composite column (so
    /// <c>Rollup(Group(a, b), c)</c> emits <c>ROLLUP((a, b), c)</c>). Emitted as the
    /// standard function form <c>ROLLUP(a, b)</c> on every dialect.
    /// </summary>
    /// <param name="element">The first grouping element.</param>
    /// <param name="elements">Further grouping elements.</param>
    /// <returns>A <c>ROLLUP</c> grouping for a <c>GROUP BY</c> clause.</returns>
    /// <remarks>
    /// MySQL accepts only its <c>WITH ROLLUP</c> suffix instead — use
    /// <c>.GroupBy(...).WithRollup()</c> for that. On a dialect that does not
    /// support the function form the grouping is emitted as written, leaving the
    /// statement for the database to reject.
    /// </remarks>
    public static RollupGrouping Rollup(object element, params object[] elements) =>
        new(GroupByItemResolver.ResolveElements([element, .. elements]));

    /// <summary>
    /// The <c>ROUND(expr)</c> function: rounds <paramref name="expr"/> to the
    /// nearest integer.
    /// </summary>
    /// <param name="expr">The numeric expression to round.</param>
    /// <returns>A <c>ROUND</c> function expression.</returns>
    public static RoundFunction Round(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Round(object)"/>
    /// <param name="expr">The numeric expression to round.</param>
    /// <param name="decimals">The number of decimal places to round to.</param>
    /// <returns>A <c>ROUND(x, n)</c> function expression.</returns>
    public static RoundFunction Round(
        object expr,
        object decimals) => new(
            Resolve(expr),
            Resolve(decimals));

    /// <summary>
    /// The <c>RPAD(source, length)</c> function: right-pads <paramref name="source"/>
    /// with spaces to <paramref name="length"/> characters.
    /// </summary>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target length.</param>
    /// <returns>An <c>RPAD</c> function expression.</returns>
    public static RpadFunction Rpad(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    /// <inheritdoc cref="Rpad(object, object)"/>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target length.</param>
    /// <param name="padding">The padding string used instead of spaces.</param>
    public static RpadFunction Rpad(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    /// <summary>
    /// The <c>RTRIM(source)</c> function: removes trailing spaces from
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>An <c>RTRIM</c> function expression.</returns>
    public static RtrimFunction Rtrim(object source) =>
        new(Resolve(source));

    /// <inheritdoc cref="Rtrim(object)"/>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChars">The set of characters to strip instead of spaces.</param>
    public static RtrimFunction Rtrim(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));

    /// <summary>
    /// The <c>REGEXP_SUBSTR(source, pattern)</c> function: the first substring of
    /// <paramref name="source"/> matching <paramref name="pattern"/>.
    /// </summary>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <returns>A <c>REGEXP_SUBSTR</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    /// <inheritdoc cref="RegexpSubstr(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    /// <inheritdoc cref="RegexpSubstr(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="occurrence">Which match to return (1-based).</param>
    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            Resolve(occurrence));

    /// <inheritdoc cref="RegexpSubstr(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="occurrence">Which match to return (1-based).</param>
    /// <param name="options">Match modifiers, emitted as Oracle's flag literal (e.g. <c>'i'</c>).</param>
    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position,
        object occurrence,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            Resolve(occurrence),
            options);

    /// <inheritdoc cref="RegexpSubstr(object, object)"/>
    /// <param name="source">The string searched.</param>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <param name="position">The 1-based character position to start searching from.</param>
    /// <param name="occurrence">Which match to return (1-based).</param>
    /// <param name="options">Match modifiers, emitted as Oracle's flag literal (e.g. <c>'i'</c>).</param>
    /// <param name="subPatternPos">The capture-group number to return instead of the whole match.</param>
    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position,
        object occurrence,
        RegexpOptions options,
        object subPatternPos) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            Resolve(occurrence),
            options,
            Resolve(subPatternPos));
}
