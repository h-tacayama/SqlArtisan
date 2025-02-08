namespace InlineSqlSharp;

internal sealed class InConditionCore(
	bool isNot,
	IExpr leftSide,
	IExpr[] expressions)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr[] _expressions = expressions;

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
		buffer.AppendCommaSeparated(_expressions);
		buffer.AppendLine();
		buffer.Append(")");
	}
}
