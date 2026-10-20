namespace SqlArtisan.Internal;

internal sealed class HammingDistanceOperator : BinaryOperator
{
    internal HammingDistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.HammingDistance, rightSide) { }
}
