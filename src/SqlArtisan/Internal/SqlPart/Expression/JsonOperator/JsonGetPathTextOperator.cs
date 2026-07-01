namespace SqlArtisan.Internal;

public sealed class JsonGetPathTextOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonHashArrowText, rightSide)
{
}
