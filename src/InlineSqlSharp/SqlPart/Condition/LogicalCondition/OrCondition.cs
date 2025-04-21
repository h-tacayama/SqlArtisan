namespace InlineSqlSharp;

public sealed class OrCondition(AbstractCondition[] conditions) :
    AbstractCondition
{
    private readonly MultiLogicalConditionCore _core = new(
        Keywords.OR,
        conditions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
