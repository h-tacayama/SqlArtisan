namespace InlineSqlSharp;

internal sealed class InSubqueryConditionCore(
	bool isNot,
	IExpr leftSide,
	ISubquery subquey)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly ISubquery _subquery = subquey;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.AppendSpace();

		if (_isNot)
		{
			buffer.AppendSpace(Keywords.NOT);
		}

		buffer.AppendLine(Keywords.IN);
		buffer.AppendLine("(");
		_subquery.FormatSql(ref buffer);
		buffer.AppendLine();
		buffer.Append(")");
	}
}
