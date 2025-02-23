namespace InlineSqlSharp;

public sealed class RegexpLikeCondition(
	CharacterExpr source,
	string pattern,
	RegexpOptions options) : ICondition
{
	private readonly CharacterExpr _source = source;
	private readonly string _pattern = pattern;
	private readonly RegexpOptions _options = options;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REGEXP_LIKE)
		.OpenParenthesis()
		.AppendComma(_source)
		.EncloseInSingleQuotes(_pattern, true)
		.PrependCommmaIf(!_options.IsNone(), _options.ToSql())
		.CloseParenthesis();
}
