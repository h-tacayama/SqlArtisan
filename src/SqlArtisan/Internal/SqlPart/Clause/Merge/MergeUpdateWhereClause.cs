namespace SqlArtisan.Internal;

// Oracle's `WHERE <condition>` on a WHEN MATCHED UPDATE SET action: only the
// matched rows satisfying it are updated, and a DELETE WHERE sees only those.
internal sealed class MergeUpdateWhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(
            _condition,
            "A MERGE UPDATE WHERE clause requires a condition.");

        buffer
            .Append($"{Keywords.Where} ")
            .Append(_condition);
    }
}
