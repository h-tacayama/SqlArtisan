namespace SqlArtisan.Internal;

public sealed class MultiplicationOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    ArithmeticOperator(leftSide, Operators.Asterisk, rightSide)
{
}
