namespace InlineSqlSharp;

public sealed class NumericSearchedCaseExpr(
    SearchedCaseWhenClause[] whenClauses,
    CaseElseExpr<NumericExpr> elseClause) :
    NumericExpr,
    ISearchedCaseExpression
{
    private readonly SearchedCaseExprCore<NumericExpr> _core =
        new(whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
