namespace SqlArtisan.Internal;

public sealed class JsonGetPathOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonHashArrow, rightSide)
{
}
