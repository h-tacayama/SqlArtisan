namespace SqlArtisan;

public sealed class DivisionOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    ArithmeticOperator(leftSide, Operators.Slash, rightSide)
{
}
