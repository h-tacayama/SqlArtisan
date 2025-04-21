namespace InlineSqlSharp;

public sealed class InCondition : AbstractCondition
{
    private readonly InConditionCore _core;

    internal InCondition(AbstractExpr leftSide, AbstractExpr[] expressions)
    {
        _core = new(false, leftSide, expressions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
