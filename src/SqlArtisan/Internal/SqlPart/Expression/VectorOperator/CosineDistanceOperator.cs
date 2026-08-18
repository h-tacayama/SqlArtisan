namespace SqlArtisan.Internal;

public sealed class CosineDistanceOperator : BinaryOperator
{
    internal CosineDistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.CosineDistance, rightSide) { }
}
