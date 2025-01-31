namespace InlineSqlSharp;

public sealed class GreaterThanCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.GreaterThan, rightSide)
{
}
