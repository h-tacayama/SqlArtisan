namespace SqlArtisan;

public sealed class ModulusOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    ArithmeticOperator(leftSide, Operators.Percent, rightSide)
{
}
