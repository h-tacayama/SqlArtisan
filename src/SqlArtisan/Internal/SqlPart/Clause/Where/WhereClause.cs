namespace SqlArtisan.Internal;

internal sealed class WhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // Shared by SELECT, UPDATE, and DELETE — this message surfaces for all three.
        ConditionGuard.ThrowIfEmpty(
            _condition,
            "The WHERE clause requires a condition; omit it for an unfiltered statement.");

        buffer
            .Append($"{Keywords.Where} ")
            .Append(_condition);
    }
}
