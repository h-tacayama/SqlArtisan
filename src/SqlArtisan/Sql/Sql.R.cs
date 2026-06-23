using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static AnalyticRankFunction Rank() => new();

    public static AnalyticRowNumberFunction RowNumber() => new();

    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    public static RegexpCountFunction RegexpCount(
        object source,
        object pattern,
        object position,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            options);

    public static RegexpLikeCondition RegexpLike(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpLikeCondition RegexpLike(
        object source,
        object pattern,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            options);

    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement));

    public static RegexpReplaceFunction RegexpReplace(
        object source,
        object pattern,
        object replacement,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement),
            Resolve(position));

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
    /// standard function form <c>ROLLUP(a, b)</c> on every dialect. MySQL accepts
    /// only its <c>WITH ROLLUP</c> suffix instead — use
    /// <c>.GroupBy(...).WithRollup()</c> for that. An unsupported target is emitted
    /// as written, leaving the statement for the database to reject.
    /// </summary>
    public static RollupGrouping Rollup(object element, params object[] elements) =>
        new(GroupByItemResolver.ResolveElements([element, .. elements]));

    /// <summary>
    /// The <c>ROUND(expr)</c> function: rounds <paramref name="expr"/> to the
    /// nearest integer.
    /// </summary>
    public static RoundFunction Round(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>ROUND(expr, decimals)</c> function: rounds <paramref name="expr"/>
    /// to <paramref name="decimals"/> decimal places.
    /// </summary>
    public static RoundFunction Round(
        object expr,
        object decimals) => new(
            Resolve(expr),
            Resolve(decimals));

    public static RpadFunction Rpad(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static RpadFunction Rpad(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static RtrimFunction Rtrim(object source) =>
        new(Resolve(source));

    public static RtrimFunction Rtrim(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));

    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    public static RegexpSubstrFunction RegexpSubstr(
        object source,
        object pattern,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            Resolve(occurrence));

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
