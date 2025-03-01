namespace InlineSqlSharp;

public sealed class ToDateFunction(CharacterExpr text, string format) : DateTimeExpr
{
	private readonly CharacterExpr _text = text;
	private readonly string _format = format;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.TO_DATE)
		.OpenParenthesis(_text)
		.Append(", ")
		.Append(new CharacterLiteral(_format))
		.CloseParenthesis();
}
