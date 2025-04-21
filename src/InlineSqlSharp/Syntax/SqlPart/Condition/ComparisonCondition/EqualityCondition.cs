namespace InlineSqlSharp;

public sealed class EqualityCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractEqualityCondition
{
    private readonly ComparisonConditionCore _core =
        new(leftSide, Operators.Equality, rightSide);

    internal override AbstractExpr LeftSide => leftSide;

    internal override AbstractExpr RightSide => rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
