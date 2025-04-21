namespace InlineSqlSharp;

internal sealed class LessThanCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.LessThan, rightSide);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
