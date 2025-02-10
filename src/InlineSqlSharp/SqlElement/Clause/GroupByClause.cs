namespace InlineSqlSharp;

public sealed class GroupByClause(IExpr[] groupingExpressions) : ISqlElement
{
	private readonly IExpr[] _groupingExpressions = groupingExpressions;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core
			.AppendSpace(Keywords.GROUP)
			.AppendLine(Keywords.BY)
			.AppendCommaSeparated(_groupingExpressions);
}
