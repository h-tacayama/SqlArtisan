namespace InlineSqlSharp;

public sealed class DateTimeColumn(AliasName tableAlias, string name) :
	DateTimeExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, name);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
