namespace SqlArtisan.Internal;

public sealed class JsonHashArrowTextOperator(
    SqlExpression leftSide,
    SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonHashArrowText, rightSide);
