namespace InlineSqlSharp;

internal sealed class InnerJoinClause(ITableReference table) : ISqlElement
{
	private readonly JoinClauseCore _core = new(Keywords.INNER, table);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
