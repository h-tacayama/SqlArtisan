namespace SqlArtisan.Internal;

public sealed class CosineDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.CosineDistance, rightSide);
