namespace InlineSqlSharp;

public sealed class RegexpLikeCondition(
	CharacterExpr source,
	CharacterExpr pattern,
	RegexpOptions options) : ICondition
{
	private readonly CharacterExpr _source = source;
	private readonly CharacterExpr _pattern = pattern;
	private readonly RegexpOptions _options = options;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REGEXP_LIKE)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_pattern)
		.PrependCommaIf(!_options.IsNone(), _options.ToSql())
		.CloseParenthesis();
}
