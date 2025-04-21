namespace InlineSqlSharp;

public sealed class DynamicCondition : AbstractCondition
{
    private readonly AbstractCondition _condition;

    internal DynamicCondition(bool addIf, AbstractCondition condition)
    {
        AddIf = addIf;
        _condition = condition;
    }

    internal bool AddIf { get; }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        if (!AddIf)
        {
            return;
        }

        _condition.FormatSql(buffer);
    }
}
