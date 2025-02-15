namespace InlineSqlSharp;

internal sealed class InSubqueryConditionCore(
	bool isNot,
	IExpr leftSide,
	ISubquery subquey)
{
	private readonly bool _isNot = isNot;
	private readonly IExpr _leftSide = leftSide;
	private readonly ISubquery _subquery = subquey;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(_leftSide)
		.AppendSpaceIf(_isNot, Keywords.NOT)
		.AppendLine(Keywords.IN)
		.EncloseInParentheses(_subquery);
}
