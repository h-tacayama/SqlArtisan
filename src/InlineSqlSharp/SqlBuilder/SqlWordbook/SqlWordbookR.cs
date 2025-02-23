namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static RegexpLikeCondition REGEXP_LIKE(
		CharacterExpr source,
		string pattern,
		RegexpOptions options = RegexpOptions.None) =>
		new(source, pattern, options);
}
