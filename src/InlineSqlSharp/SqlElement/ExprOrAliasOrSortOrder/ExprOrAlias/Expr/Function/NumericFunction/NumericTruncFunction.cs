namespace InlineSqlSharp;

public sealed class NumericTruncFunction(
	NumericExpr expr,
	NumericExpr? decimalPlaces = null) : NumericExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.TRUNC, expr, decimalPlaces);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
