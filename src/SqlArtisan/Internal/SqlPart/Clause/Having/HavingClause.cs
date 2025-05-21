namespace SqlArtisan.Internal;

internal sealed class HavingClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Having} ")
        .Append(_condition);
}
