namespace InlineSqlSharp;

internal sealed class InConditionCore(
	bool isNot,
	IExpr leftSide,
	IExpr primaryExpr,
	IExpr[] secondaryExprs)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr _primaryItem = primaryExpr;
	private readonly IExpr[] _secondaryItems = secondaryExprs;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.Append(" ");

		if (_isNot)
		{
			buffer.AppendFormat("{0} ", Keywords.NOT);
		}

		buffer.AppendLine(Keywords.IN);
		buffer.AppendLine("(");

		_primaryItem.FormatSql(ref buffer);

		for (int i = 0; i < _secondaryItems.Length; i++)
		{
			buffer.AppendLine();
			buffer.Append(", ");
			_secondaryItems[i].FormatSql(ref buffer);
		}

		buffer.AppendLine();
		buffer.Append(")");
	}
}
