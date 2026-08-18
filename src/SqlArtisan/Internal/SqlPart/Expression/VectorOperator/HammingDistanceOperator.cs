namespace SqlArtisan.Internal;

public sealed class HammingDistanceOperator : BinaryOperator
{
    internal HammingDistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.HammingDistance, rightSide) { }
}
