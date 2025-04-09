namespace InlineSqlSharp;

public sealed class AndCondition(ICondition[] conditions) : ICondition
{
    private readonly MultiLogicalConditionCore _core = new(
        Keywords.AND,
        conditions);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
