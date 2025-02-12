namespace InlineSqlSharp;

public sealed class DateTimeColumn(string tableAlias, string columnName)
	: DateTimeExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, columnName);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
