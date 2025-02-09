namespace InlineSqlSharp;

public sealed class InnerJoinClause(ITableReference table) : ISqlElement
{
	private readonly ITableReference _table = table;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendSpace(Keywords.INNER)
			.AppendLine(Keywords.JOIN)
			.Append(_table);
}
