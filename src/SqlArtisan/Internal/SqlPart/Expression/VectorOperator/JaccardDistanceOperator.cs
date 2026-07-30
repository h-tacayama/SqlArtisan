namespace SqlArtisan.Internal;

public sealed class JaccardDistanceOperator(SqlExpression leftSide, SqlExpression rightSide) :
    VectorOperator(leftSide, Operators.JaccardDistance, rightSide);
