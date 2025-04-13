namespace InlineSqlSharp;

public sealed class NumericSimpleCaseExpr(
    object expr,
    SimpleCaseWhenClause[] whenClauses,
    CaseElseExpr<NumericExpr> elseClause) :
    NumericExpr,
    ISimpleCaseExpression
{
    private readonly SimpleCaseExprCore<NumericExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
