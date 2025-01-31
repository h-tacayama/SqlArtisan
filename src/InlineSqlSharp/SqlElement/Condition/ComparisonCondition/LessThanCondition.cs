namespace InlineSqlSharp;

public sealed class LessThanCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.LessThan, rightSide)
{
}
