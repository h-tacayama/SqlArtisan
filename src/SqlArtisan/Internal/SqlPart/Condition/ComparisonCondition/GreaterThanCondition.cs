namespace SqlArtisan.Internal;

internal sealed class GreaterThanCondition(
    SqlExpression leftSide,
    SqlExpression rightSide) : SqlCondition
{
    private readonly SqlExpression _leftSide = leftSide;
    private readonly SqlExpression _rightSide = rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Operators.GreaterThan} ")
        .Append(_rightSide);
}
