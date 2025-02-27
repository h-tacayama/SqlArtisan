namespace InlineSqlSharp;

public sealed class DateTimeMinFunction(DateTimeExpr expr) : DateTimeExpr
{
	private readonly MinFunctionCore<DateTimeExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
