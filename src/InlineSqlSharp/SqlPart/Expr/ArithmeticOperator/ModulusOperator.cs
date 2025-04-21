namespace InlineSqlSharp;

internal sealed class ModulusOperator(
    AbstractExpr leftSide,
    AbstractExpr rightSide) :
    ArithmeticOperator(leftSide, Operators.Percent, rightSide)
{
}
