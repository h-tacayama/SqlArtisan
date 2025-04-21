namespace InlineSqlSharp;

public sealed class OrCondition : AbstractCondition
{
    private readonly MultiLogicalConditionCore _core;

    internal OrCondition(AbstractCondition[] conditions)
    {
        _core = new(Keywords.OR, conditions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
