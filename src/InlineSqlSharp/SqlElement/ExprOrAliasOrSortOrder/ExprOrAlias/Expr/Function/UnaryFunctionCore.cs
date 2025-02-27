namespace InlineSqlSharp;

internal sealed class UnaryFunctionCore(
	string functionName,
	IExpr expr)
{
	private readonly string _functionName = functionName;
	private readonly IExpr _expr = expr;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(_functionName)
		.OpenParenthesis()
		.Append(_expr)
		.CloseParenthesis();
}
