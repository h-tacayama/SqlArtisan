namespace SqlArtisan.Internal;

public sealed class JsonHashArrowOperator(SqlExpression leftSide, SqlExpression rightSide) :
    JsonOperator(leftSide, Operators.JsonHashArrow, rightSide);
