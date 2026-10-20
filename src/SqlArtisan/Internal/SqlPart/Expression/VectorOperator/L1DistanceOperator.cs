namespace SqlArtisan.Internal;

internal sealed class L1DistanceOperator : BinaryOperator
{
    internal L1DistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.L1Distance, rightSide) { }
}
