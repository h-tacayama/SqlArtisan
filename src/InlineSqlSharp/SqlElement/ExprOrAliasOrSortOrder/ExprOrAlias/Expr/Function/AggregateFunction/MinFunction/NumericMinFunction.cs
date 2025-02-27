namespace InlineSqlSharp;

public sealed class NumericMinFunction(NumericExpr expr) : NumericExpr
{
	private readonly MinFunctionCore<NumericExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
