namespace InlineSqlSharp;

internal sealed class FromClause(ITableReference[] tables) : ISqlElement
{
	private readonly ITableReference[] _tables = tables;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.FROM)
		.AppendCsv(_tables);
}
