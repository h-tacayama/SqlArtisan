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

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.FormatSql(_leftSide)
			.AppendSpace()
			.AppendSpaceIf(_isNot, Keywords.NOT)
			.AppendSpace(Keywords.BETWEEN)
			.FormatSql(_rightSide1)
			.EncloseInSpaces(Keywords.AND)
			.FormatSql(_rightSide2);
}
