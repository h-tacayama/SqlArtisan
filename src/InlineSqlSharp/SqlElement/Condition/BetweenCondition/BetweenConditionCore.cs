namespace InlineSqlSharp;

public sealed class BetweenConditionCore(
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

		if (_isNot)
		{
			buffer.AppendFormat(" {0} {1} ", Keywords.NOT, Keywords.BETWEEN);
		}
		else
		{

			buffer.AppendFormat(" {0} ", Keywords.BETWEEN);
		}
		_rightSide1.FormatSql(ref buffer);
		buffer.AppendFormat(" {0} ", Keywords.AND);
		_rightSide2.FormatSql(ref buffer);
	}
}
