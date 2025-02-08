namespace InlineSqlSharp;

internal sealed class BetweenConditionCore(
	bool isNot,
	IExpr leftSide,
	IExpr rightSide1,
	IExpr rightSide2)
{
	private bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr _rightSide1 = rightSide1;
	private readonly IExpr _rightSide2 = rightSide2;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.AppendSpace();

		if (_isNot)
		{
			buffer.AppendSpace(Keywords.NOT);
		}

		buffer.AppendSpace(Keywords.BETWEEN);
		_rightSide1.FormatSql(ref buffer);
		buffer.EncloseInSpaces(Keywords.AND);
		_rightSide2.FormatSql(ref buffer);
	}
}
