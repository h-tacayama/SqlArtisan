namespace InlineSqlSharp;

public sealed class InSubqueryCondition(
    IExpr leftSide,
    ISubquery subquey) : ICondition
{
    private readonly InSubqueryConditionCore _core = new(false, leftSide, subquey);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
