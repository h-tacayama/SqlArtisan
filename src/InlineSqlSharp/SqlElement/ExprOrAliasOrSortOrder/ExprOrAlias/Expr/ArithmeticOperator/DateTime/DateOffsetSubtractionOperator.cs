namespace InlineSqlSharp;

public sealed class DateOffsetSubtractionOperator(
	DateTimeExpr leftSide,
	NumericExpr rightSide)
	: DateOffsetArithmeticOperator(leftSide, Operators.Minus, rightSide)
{
}
