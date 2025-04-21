namespace InlineSqlSharp;

public sealed class NotInSubqueryCondition : AbstractCondition
{
    private readonly InSubqueryConditionCore _core;

    internal NotInSubqueryCondition(AbstractExpr leftSide, ISubquery subquey)
    {
        _core = new(true, leftSide, subquey);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
