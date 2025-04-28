namespace InlineSqlSharp;

internal sealed class GreaterThanOrEqualCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr _rightSide = rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Operators.GreaterThanOrEqual} ")
        .Append(_rightSide);
}
