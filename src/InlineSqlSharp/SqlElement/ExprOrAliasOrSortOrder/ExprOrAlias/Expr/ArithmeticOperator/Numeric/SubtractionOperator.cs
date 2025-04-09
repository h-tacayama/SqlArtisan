namespace InlineSqlSharp;

public sealed class SubtractionOperator(
    NumericExpr leftSide,
    NumericExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Minus, rightSide)
{
}
