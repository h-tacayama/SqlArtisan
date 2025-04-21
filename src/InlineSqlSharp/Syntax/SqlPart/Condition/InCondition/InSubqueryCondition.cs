namespace InlineSqlSharp;

public sealed class InSubqueryCondition(
    AbstractExpr leftSide,
    ISubquery subquey) : AbstractCondition
{
    private readonly InSubqueryConditionCore _core =
        new(false, leftSide, subquey);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
