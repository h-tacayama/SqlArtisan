namespace InlineSqlSharp;

public sealed class DateOffsetAdditionOperator(
	DateTimeExpr leftSide,
	NumericExpr rightSide)
	: DateOffsetArithmeticOperator(leftSide, Operators.Plus, rightSide)
{
}
