namespace SqlArtisan;

internal sealed class DivisionOperator(
    AbstractExpr leftSide,
    AbstractExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Slash, rightSide)
{
}
