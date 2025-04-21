using static InlineSqlSharp.ExprRsolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static AnalyticRankFunction RANK() => new();

    public static AnalyticRowNumberFunction ROW_NUMBER() => new();

    public static RegexpCountFunction REGEXP_COUNT(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpCountFunction REGEXP_COUNT(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    public static RegexpCountFunction REGEXP_COUNT(
        object source,
        object pattern,
        object position,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            options);

    public static RegexpLikeCondition REGEXP_LIKE(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpLikeCondition REGEXP_LIKE(
        object source,
        object pattern,
        RegexpOptions options) => new(
            Resolve(source),
            Resolve(pattern),
            options);

    public static RegexpReplaceFunction REGEXP_REPLACE(
        object source,
        object pattern,
        object replacement) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement));

    public static RegexpReplaceFunction REGEXP_REPLACE(
        object source,
        object pattern,
        object replacement,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(replacement),
            Resolve(position));

    public static RegexpReplaceFunction REGEXP_REPLACE(
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

    public static RegexpReplaceFunction REGEXP_REPLACE(
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

    public static ReplaceFunction REPLACE(
        object source,
        object search,
        object replacement) => new(
            Resolve(source),
            Resolve(search),
            Resolve(replacement));

    public static RpadFunction RPAD(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static RpadFunction RPAD(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static RtrimFunction RTRIM(object source) =>
        new(Resolve(source));

    public static RtrimFunction RTRIM(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        object source,
        object pattern) => new(
            Resolve(source),
            Resolve(pattern));

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        object source,
        object pattern,
        object position) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position));

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        object source,
        object pattern,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(pattern),
            Resolve(position),
            Resolve(occurrence));

    public static RegexpSubstrFunction REGEXP_SUBSTR(
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

    public static RegexpSubstrFunction REGEXP_SUBSTR(
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
