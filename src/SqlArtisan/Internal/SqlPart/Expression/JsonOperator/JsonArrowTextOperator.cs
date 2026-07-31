namespace SqlArtisan.Internal;

public sealed class JsonArrowTextOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.JsonArrowText, rightSide);
