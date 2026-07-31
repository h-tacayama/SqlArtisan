namespace SqlArtisan.Internal;

public sealed class HammingDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    VectorOperator(leftSide, Operators.HammingDistance, rightSide);
