namespace SqlArtisan.Internal;

internal class OnClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    // A virtual rather than a message field, which would cost every join node 8 B
    // per build (ADR 0006); the message differs only by the chain that wrote it.
    private protected virtual string EmptyMessage =>
        "A JOIN's ON clause requires a condition; an unconditioned join is a CROSS JOIN.";

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(_condition, EmptyMessage);

        buffer
            .Append($"{Keywords.On} ")
            .Append(_condition);
    }
}
