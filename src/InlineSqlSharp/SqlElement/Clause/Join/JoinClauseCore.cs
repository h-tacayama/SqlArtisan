namespace InlineSqlSharp;

internal sealed class JoinClauseCore(string joinType, ITableReference table)
{
	private readonly string _joinType = joinType;	
	private readonly ITableReference _table = table;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(_joinType)
		.AppendSpace(Keywords.JOIN)
		.Append(_table);
}
