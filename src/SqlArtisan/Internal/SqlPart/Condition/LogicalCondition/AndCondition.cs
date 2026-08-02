namespace SqlArtisan.Internal;

public sealed class AndCondition : SqlCondition
{
    private readonly LogicalConditionCore _core;

    internal AndCondition(SqlCondition leftSide, SqlCondition rightSide)
    {
        _core = new(leftSide, rightSide);
    }

    // Copy-on-write extension of an existing AndCondition by one more operand
    // (operator &, #399).
    internal AndCondition(AndCondition existing, SqlCondition additionalOperand)
    {
        _core = existing._core.Extend(additionalOperand);
    }

    internal override bool IsEmpty => _core.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) => _core.Format(buffer, Keywords.And);
}
