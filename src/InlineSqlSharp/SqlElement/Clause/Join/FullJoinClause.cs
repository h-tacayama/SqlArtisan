namespace InlineSqlSharp;

internal sealed class FullJoinClause(ITableReference table) : ISqlElement
{
	private readonly JoinClauseCore _core = new(Keywords.FULL, table);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
