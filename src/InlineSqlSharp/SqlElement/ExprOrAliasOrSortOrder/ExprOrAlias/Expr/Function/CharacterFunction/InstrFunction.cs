namespace InlineSqlSharp;

public sealed class InstrFunction(
	CharacterExpr source,
	CharacterExpr substring,
	int position = 1,
	int occurrence = 1) : NumericExpr
{
	private readonly CharacterExpr _source = source;
	private readonly CharacterExpr _substring = substring;
	private readonly int _position = position;
	private readonly int _occurrence = occurrence;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.INSTR)
		.OpenParenthesis()
		.AppendComma(_source)
		.Append(_substring)
		.PrependCommmaIf(_position != 1 || _occurrence != 1, _position.ToString())
		.PrependCommmaIf(_occurrence != 1, _occurrence.ToString())
		.CloseParenthesis();
}
