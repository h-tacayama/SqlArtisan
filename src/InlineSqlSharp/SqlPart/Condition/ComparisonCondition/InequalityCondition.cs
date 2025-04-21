namespace InlineSqlSharp;

internal sealed class InequalityCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractEqualityCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.Inequality, rightSide);

    internal override AbstractExpr LeftSide => leftSide;

    internal override AbstractExpr RightSide => rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
