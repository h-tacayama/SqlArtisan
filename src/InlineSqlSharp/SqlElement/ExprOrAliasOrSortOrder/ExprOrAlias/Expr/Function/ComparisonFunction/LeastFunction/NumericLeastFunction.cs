namespace InlineSqlSharp;

public sealed class NumericLeastFunction(NumericExpr[] expressions)
	: NumericExpr
{
	private readonly VariadicFunctionCore _core = new(Keywords.LEAST, expressions);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
