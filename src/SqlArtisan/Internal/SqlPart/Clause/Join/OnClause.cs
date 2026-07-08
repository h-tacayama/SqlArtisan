namespace SqlArtisan.Internal;

internal sealed class OnClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        EmptyConditionGuard.Reject(
            _condition,
            "A JOIN's ON clause requires a condition; use CrossJoin for an unconditional join.");

        buffer
            .Append($"{Keywords.On} ")
            .Append(_condition);
    }
}
