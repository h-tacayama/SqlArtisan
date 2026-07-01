namespace SqlArtisan.Internal;

public sealed class JsonGetOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonArrow, rightSide)
{
}
