namespace SqlArtisan;

internal sealed class AdditionOperator(
    AbstractExpr leftSide,
    AbstractExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Plus, rightSide)
{
}
