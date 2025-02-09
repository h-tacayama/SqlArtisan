namespace InlineSqlSharp;

internal sealed class InConditionCore(
	bool isNot,
	IExpr leftSide,
	IExpr[] expressions)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly IExpr[] _expressions = expressions;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendSpace(_leftSide)
			.AppendSpaceIf(_isNot, Keywords.NOT)
			.AppendLine(Keywords.IN)
			.OpenParenthesisBeforeLine()
			.AppendCommaSeparated(_expressions)
			.CloseParenthesisAfterLine();
}
