namespace InlineSqlSharp;

public sealed class SubstrFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly NumericExpr _position;
	private readonly NumericExpr? _length;

	public static SubstrFunction Of(
		CharacterExpr source,
		NumericExpr position) => new(source, position, null);

	public static SubstrFunction Of(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.SUBSTR)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_position)
		.PrependCommaIfNotNull(_length)
		.CloseParenthesis();

	private SubstrFunction(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr? length)
	{
		_source = source;
		_position = position;
		_length = length;
	}
}
