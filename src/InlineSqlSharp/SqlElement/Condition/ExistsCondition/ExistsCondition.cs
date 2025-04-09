namespace InlineSqlSharp;

public sealed class ExistsCondition(ISubquery subquery) : ICondition
{
    private readonly ExistsConditionCore _core = new(false, subquery);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
