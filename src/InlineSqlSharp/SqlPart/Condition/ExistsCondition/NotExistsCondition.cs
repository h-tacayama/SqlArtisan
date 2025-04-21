namespace InlineSqlSharp;

public sealed class NotExistsCondition(ISubquery subquery) : AbstractCondition
{
    private readonly ExistsConditionCore _core = new(true, subquery);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
