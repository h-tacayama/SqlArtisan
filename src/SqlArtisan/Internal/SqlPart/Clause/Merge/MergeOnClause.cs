namespace SqlArtisan.Internal;

// The MERGE join condition. Unlike a JOIN's ON clause, the condition is wrapped
// in parentheses: Oracle requires them, and SQL Server / PostgreSQL accept them.
internal sealed class MergeOnClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        EmptyConditionGuard.Reject(
            _condition,
            "A MERGE ON clause requires a condition.");

        buffer
            .Append($"{Keywords.On} ")
            .EncloseInParentheses(_condition);
    }
}
