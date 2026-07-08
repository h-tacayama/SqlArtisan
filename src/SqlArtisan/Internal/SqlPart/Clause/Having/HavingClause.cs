namespace SqlArtisan.Internal;

internal sealed class HavingClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    // An all-empty condition elides the whole clause — no group restriction.
    internal override bool IsEmpty => _condition.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Having} ")
        .Append(_condition);
}
