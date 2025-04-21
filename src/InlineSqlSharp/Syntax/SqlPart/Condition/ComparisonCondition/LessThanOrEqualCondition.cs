namespace InlineSqlSharp;

internal sealed class LessThanOrEqualCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.LessThanOrEqual, rightSide);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
