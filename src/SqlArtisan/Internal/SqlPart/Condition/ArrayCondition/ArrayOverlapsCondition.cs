namespace SqlArtisan.Internal;

public sealed class ArrayOverlapsCondition : ArrayCondition
{
    internal ArrayOverlapsCondition(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.ArrayOverlaps, rightSide) { }
}
