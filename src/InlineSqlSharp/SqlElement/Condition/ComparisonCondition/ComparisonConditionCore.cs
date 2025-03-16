namespace InlineSqlSharp;

internal sealed class ComparisonConditionCore(
	IExpr leftSide,
	string @operator,
	IExpr rightSide)
{
	private readonly IExpr _leftSide = leftSide;
	private readonly string _operator = @operator;
	private readonly IExpr _rightSide = rightSide;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(_leftSide)
		.EncloseInSpaces(_operator)
		.Append(_rightSide);
}
