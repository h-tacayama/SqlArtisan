namespace SqlArtisan;

internal sealed class WhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Where} ")
        .Append(_condition);
}
