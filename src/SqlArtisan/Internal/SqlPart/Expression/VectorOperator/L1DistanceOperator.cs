namespace SqlArtisan.Internal;

public sealed class L1DistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.L1Distance, rightSide);
