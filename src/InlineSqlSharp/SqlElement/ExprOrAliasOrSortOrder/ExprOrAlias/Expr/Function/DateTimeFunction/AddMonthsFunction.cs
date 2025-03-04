namespace InlineSqlSharp;

public sealed class AddMonthsFunction(
	DateTimeExpr dateTime,
	NumericExpr months) : DateTimeExpr
{
	private readonly BinaryFunctionCore _core =
		new(Keywords.ADD_MONTHS, dateTime, months);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
