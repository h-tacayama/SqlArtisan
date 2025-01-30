namespace InlineSqlSharp;

public sealed class ComparisonCondition(
	IExpr leftSide,
	string @operator,
	IExpr rightSide) : ICondition
{
	private readonly IExpr _leftSide = leftSide;
	private readonly string _operator = @operator;
	private readonly IExpr _rightSide = rightSide;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_leftSide.FormatSql(ref buffer);
		buffer.AppendFormat(" {0} ", _operator);
		_rightSide.FormatSql(ref buffer);
	}
}
