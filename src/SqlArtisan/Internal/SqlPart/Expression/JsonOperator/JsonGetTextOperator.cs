namespace SqlArtisan.Internal;

public sealed class JsonGetTextOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonArrowText, rightSide)
{
}
