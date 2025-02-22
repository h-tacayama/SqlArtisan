namespace InlineSqlSharp;

public sealed class ModulusOperator(
	NumericExpr leftSide,
	NumericExpr rightSide)
	: ArithmeticOperator(leftSide, Operators.Percent, rightSide)
{
}
