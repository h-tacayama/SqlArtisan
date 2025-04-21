namespace InlineSqlSharp;

public sealed class NotExistsCondition : AbstractCondition
{
    private readonly ExistsConditionCore _core;

    internal NotExistsCondition(ISubquery subquery)
    {
        _core = new(true, subquery);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
