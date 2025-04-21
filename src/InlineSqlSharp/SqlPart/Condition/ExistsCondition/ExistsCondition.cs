namespace InlineSqlSharp;

public sealed class ExistsCondition(ISubquery subquery) : AbstractCondition
{
    private readonly ExistsConditionCore _core = new(false, subquery);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
