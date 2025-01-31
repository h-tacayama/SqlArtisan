namespace InlineSqlSharp;

public sealed class EqualityCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.Equality, rightSide)
{
}
