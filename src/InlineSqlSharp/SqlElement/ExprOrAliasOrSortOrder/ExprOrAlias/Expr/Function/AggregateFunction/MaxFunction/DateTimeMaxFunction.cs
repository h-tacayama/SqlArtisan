namespace InlineSqlSharp;

public sealed class DateTimeMaxFunction(DateTimeExpr expr) : DateTimeExpr
{
	readonly UnaryFunctionCore _core = new(Keywords.MAX, expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
