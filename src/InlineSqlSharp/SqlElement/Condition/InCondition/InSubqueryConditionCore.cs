namespace InlineSqlSharp;

internal sealed class InSubqueryConditionCore(
	bool isNot,
	IExpr leftSide,
	ISubqueryBuilder subquey)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly ISubqueryBuilder _subquery = subquey;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		SqlCommand subquery = _subquery.AsSubquery(buffer.ParameterIndex);

		_leftSide.FormatSql(ref buffer);
		buffer.Append(" ");

		if (_isNot)
		{
			buffer.AppendFormat("{0} ", Keywords.NOT);
		}

		buffer.AppendLine(Keywords.IN);
		buffer.AppendLine("(");
		buffer.Append(subquery.Statement);
		buffer.AppendLine();
		buffer.Append(")");

		buffer.AddParameters(subquery.Parameters);
	}
}
