namespace SqlArtisan.Internal;

// Oracle's in-clause `DELETE WHERE <condition>`, appended to a WHEN MATCHED
// UPDATE SET action to remove the just-updated rows that satisfy the condition.
internal sealed class MergeDeleteWhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        EmptyConditionGuard.Reject(
            _condition,
            "A MERGE DELETE WHERE clause requires a condition.");

        buffer
            .Append($"{Keywords.Delete} {Keywords.Where} ")
            .Append(_condition);
    }
}
