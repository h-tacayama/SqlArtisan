namespace SqlArtisan.Internal;

public sealed class ArrayContainsCondition : ArrayCondition
{
    internal ArrayContainsCondition(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.ArrayContains, rightSide) { }
}
