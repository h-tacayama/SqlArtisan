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
		.AppendSpace(_leftSide)
		.AppendSpace(_operator)
		.Append(_rightSide);
}
