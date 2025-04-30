namespace SqlArtisan;

internal sealed class LessThanCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr _rightSide = rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Operators.LessThan} ")
        .Append(_rightSide);
}
