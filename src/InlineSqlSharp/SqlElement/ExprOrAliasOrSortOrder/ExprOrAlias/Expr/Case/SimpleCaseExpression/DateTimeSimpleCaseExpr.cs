namespace InlineSqlSharp;

public sealed class DateTimeSimpleCaseExpr<TComparisonExpr>(
    TComparisonExpr expr,
    SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr>[] whenClauses,
    CaseElseExpr<DateTimeExpr> elseClause) :
    DateTimeExpr,
    ISimpleCaseExpression
    where TComparisonExpr : IDataTypeExpr
{
    private readonly SimpleCaseExprCore<TComparisonExpr, DateTimeExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
