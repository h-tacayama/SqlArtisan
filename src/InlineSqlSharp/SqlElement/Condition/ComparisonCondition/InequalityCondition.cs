namespace InlineSqlSharp;

public sealed class InequalityCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.Inequality, rightSide)
{
}
