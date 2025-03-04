namespace InlineSqlSharp;

public sealed class NumericNvlFunction(
	NumericExpr expr1,
	NumericExpr expr2) : NumericExpr
{
	private readonly BinaryFunctionCore _core = new(Keywords.NVL, expr1, expr2);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
