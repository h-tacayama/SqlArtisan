namespace InlineSqlSharp;

public sealed class NumericColumn(string tableAlias, string columnName)
	: NumericExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, columnName);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
