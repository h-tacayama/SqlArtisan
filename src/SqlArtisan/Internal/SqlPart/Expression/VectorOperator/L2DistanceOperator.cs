namespace SqlArtisan.Internal;

public sealed class L2DistanceOperator : BinaryOperator
{
    internal L2DistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.L2Distance, rightSide) { }
}
