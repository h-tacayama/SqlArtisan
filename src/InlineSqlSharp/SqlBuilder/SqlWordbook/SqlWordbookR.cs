namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static AnalyticRankFunction RANK() => new();

    public static AnalyticRowNumberFunction ROW_NUMBER() => new();

    public static RegexpCountFunction REGEXP_COUNT(
        CharacterExpr source,
        CharacterExpr pattern) => new(source, pattern);

    public static RegexpCountFunction REGEXP_COUNT(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position) => new(source, pattern, position);

    public static RegexpCountFunction REGEXP_COUNT(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position,
        RegexpOptions options) => new(source, pattern, position, options);

    public static RegexpLikeCondition REGEXP_LIKE(
        CharacterExpr source,
        CharacterExpr pattern) => new(source, pattern);

    public static RegexpLikeCondition REGEXP_LIKE(
        CharacterExpr source,
        CharacterExpr pattern,
        RegexpOptions options) => new(source, pattern, options);

    public static RegexpReplaceFunction REGEXP_REPLACE(
        CharacterExpr source,
        CharacterExpr pattern,
        CharacterExpr replacement) => new(source, pattern, replacement);

    public static RegexpReplaceFunction REGEXP_REPLACE(
        CharacterExpr source,
        CharacterExpr pattern,
        CharacterExpr replacement,
        NumericExpr position) => new(source, pattern, replacement, position);

    public static RegexpReplaceFunction REGEXP_REPLACE(
        CharacterExpr source,
        CharacterExpr pattern,
        CharacterExpr replacement,
        NumericExpr position,
        NumericExpr occurrence) =>
        new(source, pattern, replacement, position, occurrence);

    public static RegexpReplaceFunction REGEXP_REPLACE(
        CharacterExpr source,
        CharacterExpr pattern,
        CharacterExpr replacement,
        NumericExpr position,
        NumericExpr occurrence,
        RegexpOptions options) =>
        new(source, pattern, replacement, position, occurrence, options);

    public static ReplaceFunction REPLACE(
        CharacterExpr source,
        CharacterExpr search,
        CharacterExpr replacement) => new(source, search, replacement);

    public static RpadFunction RPAD(
        CharacterExpr source,
        NumericExpr length) => new(source, length);

    public static RpadFunction RPAD(
        CharacterExpr source,
        NumericExpr length,
        CharacterExpr padding) => new(source, length, padding);

    public static RtrimFunction RTRIM(CharacterExpr source) => new(source);

    public static RtrimFunction RTRIM(
        CharacterExpr source,
        CharacterExpr trimChars) => new(source, trimChars);

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        CharacterExpr source,
        CharacterExpr pattern) => new(source, pattern);

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position) => new(source, pattern, position);

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position,
        NumericExpr occurrence) => new(source, pattern, position, occurrence);

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position,
        NumericExpr occurrence,
        RegexpOptions options) =>
        new(source, pattern, position, occurrence, options);

    public static RegexpSubstrFunction REGEXP_SUBSTR(
        CharacterExpr source,
        CharacterExpr pattern,
        NumericExpr position,
        NumericExpr occurrence,
        RegexpOptions options,
        NumericExpr subPatternPos) =>
        new(source, pattern, position, occurrence, options, subPatternPos);
}
