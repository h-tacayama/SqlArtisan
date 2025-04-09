namespace InlineSqlSharp;

public sealed class NumericSimpleCaseExpr<TComparisonExpr>(
    TComparisonExpr expr,
    SimpleCaseWhenClause<TComparisonExpr, NumericExpr>[] whenClauses,
    CaseElseExpr<NumericExpr> elseClause) :
    NumericExpr,
    ISimpleCaseExpression
    where TComparisonExpr : IExpr
{
    private readonly SimpleCaseExprCore<TComparisonExpr, NumericExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
