namespace SqlArtisan;

internal sealed class HavingClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Having} ")
        .Append(_condition);
}
