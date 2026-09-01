namespace SqlArtisan.Internal;

public sealed class OrCondition : SqlCondition
{
    private readonly LogicalConditionCore _core;

    internal OrCondition(SqlCondition leftSide, SqlCondition rightSide)
    {
        _core = new(leftSide, rightSide);
    }

    internal OrCondition(OrCondition existing, SqlCondition additionalOperand)
    {
        _core = existing._core.Extend(additionalOperand);
    }

    internal override bool IsEmpty => _core.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) => _core.Format(buffer, Keywords.Or);
}
