namespace InlineSqlSharp;

public sealed class PartitionAndOrderByClause : ISqlElement
{
	private readonly PartitionByClause? _partitionByClause;
	private readonly OrderByClause _orderByClause;

	private PartitionAndOrderByClause(
		PartitionByClause? partitionByClause,
		OrderByClause orderByClause)
	{
		_partitionByClause = partitionByClause;
		_orderByClause = orderByClause;
	}

	public static PartitionAndOrderByClause Of(
		PartitionByClause partitionByClause,
		OrderByClause orderByClause) =>
		new(partitionByClause, orderByClause);

	public static PartitionAndOrderByClause Of(
		OrderByClause orderByClause) =>
		new(null, orderByClause);

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLineIfNotNull(_partitionByClause)
		.Append(_orderByClause);
}
