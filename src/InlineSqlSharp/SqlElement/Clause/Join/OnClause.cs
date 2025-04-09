namespace InlineSqlSharp;

internal sealed class OnClause(ICondition condition) : ISqlElement
{
    private readonly ICondition _condition = condition;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.ON)
        .Append(_condition);
}
