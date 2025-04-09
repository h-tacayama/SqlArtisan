namespace InlineSqlSharp;

public sealed class NumericSearchedCaseExpr(
    SearchedCaseWhenClause<NumericExpr>[] whenClauses,
    CaseElseExpr<NumericExpr> elseClause) :
    NumericExpr,
    ISearchedCaseExpression
{
    private readonly SearchedCaseExprCore<NumericExpr> _core =
        new(whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
