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
