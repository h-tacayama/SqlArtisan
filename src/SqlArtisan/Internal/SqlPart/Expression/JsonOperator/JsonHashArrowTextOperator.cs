namespace SqlArtisan.Internal;

public sealed class JsonHashArrowTextOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.JsonHashArrowText, rightSide);
