namespace SqlArtisan;

internal sealed class SubtractionOperator(
    AbstractExpr leftSide,
    AbstractExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Minus, rightSide)
{
}
