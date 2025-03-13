namespace InlineSqlSharp;

public sealed class OverClause : ISqlElement
{
	private readonly PartitionByClause? _partitionByClause;
	private readonly PartitionAndOrderByClause? _partitionAndOrderByClause;

	private OverClause(
		PartitionByClause? partitionByClause = null,
		PartitionAndOrderByClause? partitionAndOrderByClause = null)
	{
		_partitionByClause = partitionByClause;
		_partitionAndOrderByClause = partitionAndOrderByClause;
	}

	internal static OverClause Of() => new();

	internal static OverClause Of(PartitionByClause partitionByClause) =>
		new(partitionByClause);

	internal static OverClause Of(PartitionAndOrderByClause partitionAndOrderByClause) =>
		new(null, partitionAndOrderByClause);

	public void FormatSql(SqlBuildingBuffer buffer)
	{
		buffer.Append(Keywords.OVER);

		if (_partitionByClause is null
			&& _partitionAndOrderByClause is null)
		{
			buffer.OpenParenthesis()
				.CloseParenthesis();
			return;
		}

		buffer.AppendLine()
			.OpenParenthesis()
			.AppendLine();

		if (_partitionAndOrderByClause is not null)
		{
			_partitionAndOrderByClause.FormatSql(buffer);
		}
		else if (_partitionByClause is not null)
		{
			_partitionByClause.FormatSql(buffer);
		}

		buffer.AppendLine()
			.CloseParenthesis();
	}
}
