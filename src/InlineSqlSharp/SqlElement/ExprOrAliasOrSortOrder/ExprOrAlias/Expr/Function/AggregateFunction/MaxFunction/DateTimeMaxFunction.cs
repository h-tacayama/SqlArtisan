namespace InlineSqlSharp;

public sealed class DateTimeMaxFunction(DateTimeExpr expr) : DateTimeExpr
{
	private readonly MaxFunctionCore<DateTimeExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
