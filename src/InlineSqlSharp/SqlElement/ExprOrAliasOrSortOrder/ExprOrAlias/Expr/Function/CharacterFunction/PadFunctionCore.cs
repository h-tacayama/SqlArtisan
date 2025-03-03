namespace InlineSqlSharp;

internal sealed class PadFunctionCore(
	string functionName,
	CharacterExpr source,
	NumericExpr length,
	CharacterExpr? padding)
{
	private readonly string _functionName = functionName;
	private readonly CharacterExpr _source = source;
	private readonly NumericExpr _length = length;
	private readonly CharacterExpr? _padding = padding;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(_functionName)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_length)
		.PrependCommaIfNotNull(_padding)
		.CloseParenthesis();
}
