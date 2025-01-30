namespace InlineSqlSharp;

public sealed class NumericColumn(AliasName tableAlias, string name) :
	NumericExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, name);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
