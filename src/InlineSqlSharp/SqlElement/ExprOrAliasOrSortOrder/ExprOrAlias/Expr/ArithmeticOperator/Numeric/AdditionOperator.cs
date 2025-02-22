namespace InlineSqlSharp;

public sealed class AdditionOperator(
	NumericExpr leftSide,
	NumericExpr rightSide)
	: ArithmeticOperator(leftSide, Operators.Plus, rightSide)
{
}
