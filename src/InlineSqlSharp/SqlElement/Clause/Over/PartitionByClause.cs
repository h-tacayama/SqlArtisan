namespace InlineSqlSharp;

public sealed class PartitionByClause(
	IExprOrAliasOrSortOrder[] expressions) : ISqlElement
{
	private readonly IExprOrAliasOrSortOrder[] _expressions = expressions;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.PARTITION)
		.AppendLine(Keywords.BY)
		.AppendCsvLines(_expressions);

	public PartitionAndOrderByClause ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions) =>
		PartitionAndOrderByClause.Of(this, new OrderByClause(sortExpressions));
}
