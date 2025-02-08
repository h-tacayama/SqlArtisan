namespace InlineSqlSharp;

public sealed class ExistsConditionCore(bool isNot, ISubqueryBuilder subquery)
{
	private readonly bool _isNot = isNot;
	private readonly ISubqueryBuilder _subquery = subquery;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		SqlCommand subquery = _subquery.AsSubquery(buffer.ParameterIndex);

		if (_isNot)
		{
			buffer.AppendSpace(Keywords.NOT);
		}

		buffer.AppendLine(Keywords.EXISTS);
		buffer.AppendLine("(");
		buffer.Append(subquery.Statement);
		buffer.PrependLine(")");

		buffer.AddParameters(subquery.Parameters);
	}
}
