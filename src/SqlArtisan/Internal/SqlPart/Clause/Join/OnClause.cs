namespace SqlArtisan.Internal;

internal sealed class OnClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(
            _condition,
            "A JOIN's ON clause requires a condition; an unconditioned join is a CROSS JOIN.");

        buffer
            .Append($"{Keywords.On} ")
            .Append(_condition);
    }
}
