namespace InlineSqlSharp;

public sealed class AndCondition : AbstractCondition
{
    private readonly MultiLogicalConditionCore _core;

    internal AndCondition(AbstractCondition[] conditions)
    {
        _core = new(Keywords.AND, conditions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
