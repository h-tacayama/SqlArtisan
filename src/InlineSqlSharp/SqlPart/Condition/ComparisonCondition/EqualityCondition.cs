namespace InlineSqlSharp;

public sealed class EqualityCondition : AbstractEqualityCondition
{
    private readonly ComparisonConditionCore _core;

    internal EqualityCondition(AbstractExpr leftSide, AbstractExpr rightSide)
    {
        _core = new(leftSide, Operators.Equality, rightSide);
        LeftSide = leftSide;
        RightSide = rightSide;
    }

    internal override AbstractExpr LeftSide { get; }

    internal override AbstractExpr RightSide { get; }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
