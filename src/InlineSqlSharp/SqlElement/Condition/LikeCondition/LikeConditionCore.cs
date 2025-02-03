namespace InlineSqlSharp;

internal sealed class LikeConditionCore(bool isNot, IExpr leftSide, IExpr rightSide)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr _rightSide = rightSide;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);

		if (_isNot)
		{
			buffer.AppendFormat(" {0} {1} ", Keywords.NOT, Keywords.LIKE);
		}
		else
		{
			buffer.AppendFormat(" {0} ", Keywords.LIKE);
		}

		_rightSide.FormatSql(ref buffer);
	}
}
