namespace SqlArtisan;

public sealed class EqualityCondition(
    SqlExpression leftSide,
    SqlExpression rightSide) : EqualityBasedCondition
{
    internal override SqlExpression LeftSide => leftSide;

    internal override SqlExpression RightSide => rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(LeftSide)
        .Append($" {Operators.Equality} ")
        .Append(RightSide);
}
