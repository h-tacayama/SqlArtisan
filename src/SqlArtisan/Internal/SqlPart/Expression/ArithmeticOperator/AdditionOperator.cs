namespace SqlArtisan.Internal;

public sealed class AdditionOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    ArithmeticOperator(leftSide, Operators.Plus, rightSide)
{
}
