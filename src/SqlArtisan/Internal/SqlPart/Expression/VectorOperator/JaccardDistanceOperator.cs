namespace SqlArtisan.Internal;

public sealed class JaccardDistanceOperator : BinaryOperator
{
    internal JaccardDistanceOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JaccardDistance, rightSide) { }
}
