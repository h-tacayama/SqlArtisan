namespace InlineSqlSharp;

public sealed class EqualCondition(
	IExpr leftSide,
	IExpr rightSide)
	: ComparisonCondition(leftSide, Operators.Equality, rightSide)
{
}
