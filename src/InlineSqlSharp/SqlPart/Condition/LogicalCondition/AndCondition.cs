namespace InlineSqlSharp;

public sealed class AndCondition(AbstractCondition[] conditions) :
    AbstractCondition
{
    private readonly MultiLogicalConditionCore _core = new(
        Keywords.AND,
        conditions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
