namespace InlineSqlSharp;

public sealed class NumberColumn(AliasName tableAlias, string name) :
	NumberExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, name);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
