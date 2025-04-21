namespace InlineSqlSharp;

internal sealed class GreaterThanOrEqualCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.GreaterThanOrEqual, rightSide);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
