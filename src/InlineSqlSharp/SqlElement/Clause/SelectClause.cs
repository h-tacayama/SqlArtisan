namespace InlineSqlSharp;

public sealed class SelectClause(IExprOrAlias[] selectList) : ISqlElement
{
	private readonly IExprOrAlias[] _selectList = selectList;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		buffer.AppendLine(Keywords.SELECT);
		buffer.AppendCommaSeparated(_selectList);
	}
}
