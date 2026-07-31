namespace SqlArtisan.Internal;

public sealed class HammingDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.HammingDistance, rightSide);
