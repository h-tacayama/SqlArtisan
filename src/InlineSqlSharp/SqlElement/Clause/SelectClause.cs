namespace InlineSqlSharp;

public sealed class SelectClause(
	bool distinct,
	IExprOrAlias[] selectList) : ISqlElement
{
	private readonly bool _distinct = distinct;
	private readonly IExprOrAlias[] _selectList = selectList;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.SELECT)
		.AppendLineIf(_distinct, Keywords.DISTINCT)
		.AppendCommaSeparated(_selectList);
}
