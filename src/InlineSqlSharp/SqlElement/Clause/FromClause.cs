namespace InlineSqlSharp;

public sealed class FromClause(ITableReference[] tables) : ISqlElement
{
	private readonly ITableReference[] _tables = tables;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.FROM)
		.AppendCommaSeparated(_tables);
}
