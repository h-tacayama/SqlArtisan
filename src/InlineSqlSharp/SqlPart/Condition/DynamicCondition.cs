namespace InlineSqlSharp;

public sealed class DynamicCondition(
    bool addIf,
    AbstractCondition condition) : AbstractCondition
{
    private readonly AbstractCondition _condition = condition;

    internal bool AddIf { get; } = addIf;

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        if (!AddIf)
        {
            return;
        }

        _condition.FormatSql(buffer);
    }
}
