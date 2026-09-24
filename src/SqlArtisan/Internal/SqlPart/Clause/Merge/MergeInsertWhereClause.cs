namespace SqlArtisan.Internal;

// Oracle's `WHERE <condition>` on a WHEN NOT MATCHED INSERT action: only the
// unmatched source rows satisfying it are inserted.
internal sealed class MergeInsertWhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(
            _condition,
            "A MERGE INSERT WHERE clause requires a condition.");

        buffer
            .Append($"{Keywords.Where} ")
            .Append(_condition);
    }
}
