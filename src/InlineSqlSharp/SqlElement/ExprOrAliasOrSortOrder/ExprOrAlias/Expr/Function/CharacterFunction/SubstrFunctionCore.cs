namespace InlineSqlSharp;

internal sealed class SubstrFunctionCore(
	string functionName,
	CharacterExpr source,
	NumericExpr position,
	NumericExpr? length)
{
	private readonly string _functionName = functionName;
	private readonly CharacterExpr _source = source;
	private readonly NumericExpr _position = position;
	private readonly NumericExpr? _length = length;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(_functionName)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_position)
		.PrependCommaIfNotNull(_length)
		.CloseParenthesis();
}
