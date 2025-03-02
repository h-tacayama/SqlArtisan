namespace InlineSqlSharp;

public sealed class ReplaceFunction(
    CharacterExpr source,
    CharacterExpr search,
    CharacterExpr replacement) : CharacterExpr
{
	private readonly CharacterExpr _source = source;
	private readonly CharacterExpr _search = search;
	private readonly CharacterExpr _replacement = replacement;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REPLACE)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_search)
		.PrependComma(_replacement)
		.CloseParenthesis();
}