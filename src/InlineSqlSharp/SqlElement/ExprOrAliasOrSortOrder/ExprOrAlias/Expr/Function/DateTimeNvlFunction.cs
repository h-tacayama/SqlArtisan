namespace InlineSqlSharp;

public sealed class DateTimeNvlFunction(
	DateTimeExpr expr1,
	DateTimeExpr expr2) : DateTimeExpr
{
	private readonly BinaryFunctionCore _core = new(Keywords.NVL, expr1, expr2);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
