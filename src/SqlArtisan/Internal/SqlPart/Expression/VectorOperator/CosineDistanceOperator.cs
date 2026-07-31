namespace SqlArtisan.Internal;

public sealed class CosineDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    VectorOperator(leftSide, Operators.CosineDistance, rightSide);
