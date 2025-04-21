namespace InlineSqlSharp;

public sealed class NotInCondition : AbstractCondition
{
    private readonly InConditionCore _core;

    internal NotInCondition(AbstractExpr leftSide, AbstractExpr[] expressions)
    {
        _core = new(true, leftSide, expressions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
