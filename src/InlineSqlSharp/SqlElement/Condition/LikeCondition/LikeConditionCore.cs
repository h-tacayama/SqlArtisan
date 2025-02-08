namespace InlineSqlSharp;

internal sealed class LikeConditionCore(bool isNot, IExpr leftSide, IExpr rightSide)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr _rightSide = rightSide;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.AppendSpace();

		if (_isNot)
		{
			buffer.AppendSpace(Keywords.NOT);
		}

		buffer.AppendSpace(Keywords.LIKE);
		_rightSide.FormatSql(ref buffer);
	}
}
