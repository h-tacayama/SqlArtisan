namespace InlineSqlSharp;

public sealed class ToDateFunction(string text, string format) : DateTimeExpr
{
	private readonly string _text = text;
	private readonly string _format = format;

	// carete CharacterLiteral to format single quote
	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.TO_DATE)
		.OpenParenthesis(new CharacterLiteral(_text))
		.Append(", ")
		.Append(new CharacterLiteral(_format))
		.CloseParenthesis();
}
