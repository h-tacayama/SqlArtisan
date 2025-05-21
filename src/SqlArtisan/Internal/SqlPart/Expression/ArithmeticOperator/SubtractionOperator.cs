namespace SqlArtisan.Internal;

public sealed class SubtractionOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    ArithmeticOperator(leftSide, Operators.Minus, rightSide)
{
}
