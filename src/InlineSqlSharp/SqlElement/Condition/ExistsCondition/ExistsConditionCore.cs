namespace InlineSqlSharp;

public sealed class ExistsConditionCore(bool isNot, ISubquery subquery)
{
	private readonly bool _isNot = isNot;
	private readonly ISubquery _subquery = subquery;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		if (_isNot)
		{
			buffer.AppendSpace(Keywords.NOT);
		}

		buffer.AppendLine(Keywords.EXISTS);
		buffer.AppendLine("(");
		_subquery.FormatSql(ref buffer);
		buffer.PrependLine(")");
	}
}
