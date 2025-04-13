namespace InlineSqlSharp;

public sealed class SearchedCaseWhenCondition(ICondition whenCondition) :
    ISqlElement
{
    private readonly ICondition _whenCondition = whenCondition;

    public SearchedCaseWhenClause THEN(object thenExpr) =>
        new(this, new CaseThenExpr(thenExpr));

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _whenCondition.FormatSql(buffer);
}
