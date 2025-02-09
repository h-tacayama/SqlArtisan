namespace InlineSqlSharp;

public abstract class ComparisonCondition(
	IExpr leftSide,
	string @operator,
	IExpr rightSide) : ICondition
{
	private readonly IExpr _leftSide = leftSide;
	private readonly string _operator = @operator;
	private readonly IExpr _rightSide = rightSide;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.Append(_leftSide)
			.EncloseInSpaces(_operator)
			.Append(_rightSide);
}
