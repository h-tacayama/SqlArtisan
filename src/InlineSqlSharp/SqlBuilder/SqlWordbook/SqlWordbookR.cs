namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static RegexpLikeCondition REGEXP_LIKE(
		CharacterExpr source,
		string pattern,
		RegexpOptions options = RegexpOptions.None) =>
		new(source, pattern, options);

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
