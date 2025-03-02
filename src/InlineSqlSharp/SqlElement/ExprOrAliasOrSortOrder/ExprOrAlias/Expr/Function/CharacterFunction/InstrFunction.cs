namespace InlineSqlSharp;

public sealed class InstrFunction : NumericExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr _substring;
	private readonly NumericExpr? _position;
	private readonly NumericExpr? _occurrence;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.INSTR)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_substring)
		.PrependCommaIfNotNull(_position)
		.PrependCommaIfNotNull(_occurrence)
		.CloseParenthesis();

	internal static InstrFunction Of(
		CharacterExpr source,
		CharacterExpr substring) =>
		new(source, substring, null, null);

	internal static InstrFunction Of(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position) =>
		new(source, substring, position, null);

	internal static InstrFunction Of(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr position,
		NumericExpr occurrence) =>
		new(source, substring, position, occurrence);

	private InstrFunction(
		CharacterExpr source,
		CharacterExpr substring,
		NumericExpr? position,
		NumericExpr? occurrence)
	{
		_source = source;
		_substring = substring;
		_position = position;
		_occurrence = ArgumentValidator.ThrowIfPrecedingNull(
			position,
			occurrence);
	}
}
