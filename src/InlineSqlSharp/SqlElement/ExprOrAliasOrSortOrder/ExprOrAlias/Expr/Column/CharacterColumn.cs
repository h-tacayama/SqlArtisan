namespace InlineSqlSharp;

public sealed class CharacterColumn(AliasName tableAlias, string name) :
	CharacterExpr,
	IColumn
{
	private readonly ColumnCore _core = new(tableAlias, name);

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
