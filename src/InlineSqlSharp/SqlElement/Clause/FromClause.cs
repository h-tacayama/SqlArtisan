namespace InlineSqlSharp;

public sealed class FromClause(ITableReference[] tables) : ISqlElement
{
	private readonly ITableReference[] _tables = tables;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		buffer.AppendLine(Keywords.FROM);
		buffer.AppendCommaSeparated(_tables);
	}
}
