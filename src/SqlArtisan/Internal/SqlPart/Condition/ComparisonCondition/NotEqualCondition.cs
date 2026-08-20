namespace SqlArtisan.Internal;

internal sealed class NotEqualCondition(
    SqlExpression leftSide,
    SqlExpression rightSide) : EqualityCondition
{
    internal override SqlExpression LeftSide => leftSide;

    internal override SqlExpression RightSide => rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(LeftSide)
        .EncloseInSpaces(Operators.NotEqual)
        .Append(RightSide);
}
