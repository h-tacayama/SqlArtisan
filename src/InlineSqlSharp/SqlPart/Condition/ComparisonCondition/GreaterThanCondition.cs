namespace InlineSqlSharp;

internal sealed class GreaterThanCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.GreaterThan, rightSide);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
