namespace InlineSqlSharp;

internal sealed class GreaterThanCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr _rightSide = rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Operators.GreaterThan} ")
        .Append(_rightSide);
}
