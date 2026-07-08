namespace SqlArtisan.Internal;

internal sealed class WhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // A written WHERE with no runnable condition (every operand excluded) is
        // rejected rather than silently dropped — omit .Where(...) for an
        // unfiltered statement (the #236 empty-state policy). Shared by SELECT,
        // UPDATE, and DELETE; the aggregate FILTER intercepts with its own message.
        EmptyConditionGuard.Reject(
            _condition,
            "The WHERE clause requires a condition; omit it for an unfiltered statement.");

        buffer
            .Append($"{Keywords.Where} ")
            .Append(_condition);
    }
}
