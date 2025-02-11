namespace InlineSqlSharp;

public sealed class JoinClauseCore(string joinType, ITableReference table)
{
	private readonly string _joinType = joinType;	
	private readonly ITableReference _table = table;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.AppendSpace(_joinType)
			.AppendLine(Keywords.JOIN)
			.Append(_table);
}
