namespace InlineSqlSharp;

public sealed class NotInSubqueryCondition(
    AbstractExpr leftSide,
    ISubquery subquey) : AbstractCondition
{
    private readonly InSubqueryConditionCore _core =
        new(true, leftSide, subquey);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
