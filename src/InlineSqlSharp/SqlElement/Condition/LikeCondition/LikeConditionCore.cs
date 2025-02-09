namespace InlineSqlSharp;

internal sealed class LikeConditionCore(bool isNot, IExpr leftSide, IExpr rightSide)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr _rightSide = rightSide;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendSpace(_leftSide)
		.AppendSpaceIf(_isNot, Keywords.NOT)
		.AppendSpace(Keywords.LIKE)
		.Append(_rightSide);
}
