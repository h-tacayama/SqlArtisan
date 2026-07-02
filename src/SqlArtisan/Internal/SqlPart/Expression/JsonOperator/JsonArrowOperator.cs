namespace SqlArtisan.Internal;

public sealed class JsonArrowOperator(SqlExpression leftSide, SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonArrow, rightSide);
