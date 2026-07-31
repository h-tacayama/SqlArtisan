namespace SqlArtisan.Internal;

public sealed class L2DistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.L2Distance, rightSide);
