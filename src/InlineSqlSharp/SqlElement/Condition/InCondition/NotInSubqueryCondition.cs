namespace InlineSqlSharp;

public sealed class NotInSubqueryCondition(
    IExpr leftSide,
    ISubquery subquey) : ICondition
{
    private readonly InSubqueryConditionCore _core = new(true, leftSide, subquey);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
