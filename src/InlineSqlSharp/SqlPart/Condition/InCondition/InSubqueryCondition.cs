namespace InlineSqlSharp;

public sealed class InSubqueryCondition : AbstractCondition
{
    private readonly InSubqueryConditionCore _core;

    internal InSubqueryCondition(AbstractExpr leftSide, ISubquery subquey)
    {
        _core = new(false, leftSide, subquey);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
