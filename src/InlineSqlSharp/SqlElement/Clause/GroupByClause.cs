namespace InlineSqlSharp;

public sealed class GroupByClause(IExpr[] groupingExpressions) : ISqlElement
{
	private readonly IExpr[] _groupingExpressions = groupingExpressions;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.GROUP)
		.AppendLine(Keywords.BY)
		.AppendCsvLines(_groupingExpressions);
}
