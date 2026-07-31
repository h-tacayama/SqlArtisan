namespace SqlArtisan.Internal;

public sealed class JsonArrowOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.JsonArrow, rightSide);
