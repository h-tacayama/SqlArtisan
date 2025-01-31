namespace InlineSqlSharp;

public sealed class GreaterThanOrEqualCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.GreaterThanOrEqual, rightSide)
{
}
