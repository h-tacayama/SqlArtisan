namespace InlineSqlSharp;

public sealed class DivisionOperator(
	NumericExpr leftSide,
	NumericExpr rightSide)
	: ArithmeticOperator(leftSide, Operators.Slash, rightSide)
{
}
