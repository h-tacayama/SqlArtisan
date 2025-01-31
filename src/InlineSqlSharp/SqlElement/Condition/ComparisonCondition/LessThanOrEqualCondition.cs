namespace InlineSqlSharp;

public sealed class LessThanOrEqualCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.LessThanOrEqual, rightSide)
{
}
