namespace SqlArtisan.Internal;

internal sealed class HavingClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // A written HAVING with every operand excluded is rejected rather than
        // silently dropped — omit .Having(...) for no group restriction.
        EmptyConditionGuard.Reject(
            _condition,
            "The HAVING clause requires a condition; omit it for no group restriction.");

        buffer
            .Append($"{Keywords.Having} ")
            .Append(_condition);
    }
}
