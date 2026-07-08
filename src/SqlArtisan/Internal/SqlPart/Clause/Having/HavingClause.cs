namespace SqlArtisan.Internal;

internal sealed class HavingClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        EmptyConditionGuard.Reject(
            _condition,
            "The HAVING clause requires a condition; omit it for no group restriction.");

        buffer
            .Append($"{Keywords.Having} ")
            .Append(_condition);
    }
}
