namespace InlineSqlSharp;

public sealed class DateTimeLeastFunction(params DateTimeExpr[] expressions)
	: DateTimeExpr
{
	private readonly VariadicFunctionCore _core = new(Keywords.LEAST, expressions);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
