namespace SqlArtisan.Internal;

public sealed class JsonHashArrowOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.JsonHashArrow, rightSide);
