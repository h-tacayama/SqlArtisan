namespace InlineSqlSharp;

public sealed class SimpleCaseWhenExpr<TWhenExpr>(TWhenExpr whenExpr) :
    ISqlElement
    where TWhenExpr : IDataTypeExpr
{
    private readonly TWhenExpr _whenExpr = whenExpr;

    public SimpleCaseWhenClause<TWhenExpr, CharacterExpr> THEN(CharacterExpr thenExpr) =>
        new(this, new CaseThenExpr<CharacterExpr>(thenExpr));

    public SimpleCaseWhenClause<TWhenExpr, DateTimeExpr> THEN(DateTimeExpr thenExpr) =>
        new(this, new CaseThenExpr<DateTimeExpr>(thenExpr));

    public SimpleCaseWhenClause<TWhenExpr, NumericExpr> THEN(NumericExpr thenExpr) =>
        new(this, new CaseThenExpr<NumericExpr>(thenExpr));

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _whenExpr.FormatSql(buffer);
}
