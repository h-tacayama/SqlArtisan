namespace InlineSqlSharp;

internal sealed class CrossJoinClause(ITableReference table) : ISqlElement
{
	private readonly JoinClauseCore _core = new(Keywords.CROSS, table);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
