namespace InlineSqlSharp;

public sealed class LeftJoinClause(ITableReference table) : ISqlElement
{
	private readonly JoinClauseCore _core = new(Keywords.LEFT, table);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
