namespace InlineSqlSharp;

public sealed class SearchedCaseWhenCondition(ICondition whenCondition) :
    ISqlElement
{
    private readonly ICondition _whenCondition = whenCondition;

    public SearchedCaseWhenClause<CharacterExpr> THEN(CharacterExpr thenExpr) =>
        new(this, new CaseThenExpr<CharacterExpr>(thenExpr));

    public SearchedCaseWhenClause<DateTimeExpr> THEN(DateTimeExpr thenExpr) =>
        new(this, new CaseThenExpr<DateTimeExpr>(thenExpr));

    public SearchedCaseWhenClause<NumericExpr> THEN(NumericExpr thenExpr) =>
        new(this, new CaseThenExpr<NumericExpr>(thenExpr));

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _whenCondition.FormatSql(buffer);
}
