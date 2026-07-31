namespace SqlArtisan.Internal;

public sealed class JaccardDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.JaccardDistance, rightSide);
