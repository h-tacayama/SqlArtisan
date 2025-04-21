namespace InlineSqlSharp;

public sealed class ExistsCondition : AbstractCondition
{
    private readonly ExistsConditionCore _core;

    internal ExistsCondition(ISubquery subquery)
    {
        _core = new(false, subquery);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
