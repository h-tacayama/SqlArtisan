namespace InlineSqlSharp;

public sealed class NumericMaxFunction(NumericExpr expr) : NumericExpr
{
	private readonly MaxFunctionCore<NumericExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
