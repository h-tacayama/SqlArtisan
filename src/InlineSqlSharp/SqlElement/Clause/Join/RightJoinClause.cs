namespace InlineSqlSharp;

internal sealed class RightJoinClause(ITableReference table) : ISqlElement
{
	private readonly JoinClauseCore _core = new(Keywords.RIGHT, table);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
