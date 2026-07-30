namespace SqlArtisan.Internal;

public sealed class L2DistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    VectorOperator(leftSide, Operators.L2Distance, rightSide);
