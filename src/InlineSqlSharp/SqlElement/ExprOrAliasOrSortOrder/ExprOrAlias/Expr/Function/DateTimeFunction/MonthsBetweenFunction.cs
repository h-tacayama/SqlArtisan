namespace InlineSqlSharp;

public sealed class MonthsBetweenFunction(
	DateTimeExpr date1,
	DateTimeExpr date2) : NumericExpr
{
	private readonly BinaryFunctionCore _core =
		new(Keywords.MONTHS_BETWEEN, date1, date2);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
