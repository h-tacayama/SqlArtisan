namespace InlineSqlSharp;

public sealed class MultiplicationOperator(
	NumericExpr leftSide,
	NumericExpr rightSide)
	: ArithmeticOperator(leftSide, Operators.Asterisk, rightSide)
{
}
