namespace InlineSqlSharp;

public sealed class PartitionByAndOrderBy(
	PartitionByClause partitionByClause,
	OrderByClause orderByClause) : ISqlElement
{
	private readonly PartitionByClause _partitionByClause = partitionByClause;
	private readonly OrderByClause _orderByClause = orderByClause;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpaceIfNotNull(_partitionByClause)
		.Append(_orderByClause);
}
