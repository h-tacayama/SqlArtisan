namespace SqlArtisan.Internal;

internal sealed class ArrayContainedByCondition : ArrayCondition
{
    internal ArrayContainedByCondition(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.ArrayContainedBy, rightSide) { }
}
