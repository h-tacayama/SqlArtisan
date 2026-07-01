namespace SqlArtisan.Internal;

public sealed class JsonArrowTextOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonArrowText, rightSide);
