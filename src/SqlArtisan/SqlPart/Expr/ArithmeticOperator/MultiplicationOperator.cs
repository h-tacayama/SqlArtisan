namespace SqlArtisan;

internal sealed class MultiplicationOperator(
    AbstractExpr leftSide,
    AbstractExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Asterisk, rightSide)
{
}
