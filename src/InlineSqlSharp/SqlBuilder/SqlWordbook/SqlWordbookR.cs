namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static RegexpCountFunction REGEXP_COUNT(
		CharacterExpr source,
		CharacterExpr pattern) =>
		RegexpCountFunction.Of(source, pattern);

	public static RegexpCountFunction REGEXP_COUNT(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position,
		RegexpOptions options = RegexpOptions.None) =>
		RegexpCountFunction.Of(source, pattern, position, options);

	public static RegexpLikeCondition REGEXP_LIKE(
		CharacterExpr source,
		CharacterExpr pattern,
		RegexpOptions options = RegexpOptions.None) =>
		new(source, pattern, options);

	public static RegexpReplaceFunction REGEXP_REPLACE(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement) =>
		RegexpReplaceFunction.Of(source, pattern, replacement);

	public static RegexpReplaceFunction REGEXP_REPLACE(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement,
		NumericExpr position) =>
		RegexpReplaceFunction.Of(source, pattern, replacement, position);

	public static RegexpReplaceFunction REGEXP_REPLACE(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement,
		NumericExpr position,
		NumericExpr occurrence,
		RegexpOptions options = RegexpOptions.None) =>
		RegexpReplaceFunction.Of(source, pattern, replacement, position, occurrence, options);

	public static ReplaceFunction REPLACE(
		CharacterExpr source,
		CharacterExpr search,
		CharacterExpr replacement) => new(source, search, replacement);

	public static RpadFunction RPAD(
		CharacterExpr source,
		NumericExpr length) => RpadFunction.Of(source, length);

	public static RpadFunction RPAD(
		CharacterExpr source,
		NumericExpr length,
		CharacterExpr padding) => RpadFunction.Of(source, length, padding);

	public static CharacterExpr RTRIM(CharacterExpr source) =>
		RtrimFunction.Of(source);

	public static CharacterExpr RTRIM(
		CharacterExpr source,
		CharacterExpr trimChars) => RtrimFunction.Of(source, trimChars);
}
