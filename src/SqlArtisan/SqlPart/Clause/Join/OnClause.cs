namespace SqlArtisan;

internal sealed class OnClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.On} ")
        .Append(_condition);
}
