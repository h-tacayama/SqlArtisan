namespace InlineSqlSharp;

internal sealed class SimpleCaseExprCore<TComparisonExpr, TReturnExpr>(
    TComparisonExpr expr,
    SimpleCaseWhenClause<TComparisonExpr, TReturnExpr>[] whenClauses,
    CaseElseExpr<TReturnExpr> elseClause)
    where TComparisonExpr : IDataTypeExpr
    where TReturnExpr : IDataTypeExpr
{
    private readonly TComparisonExpr _expr = expr;
    private readonly SimpleCaseWhenClause<TComparisonExpr, TReturnExpr>[] _whenClauses = whenClauses;
    private readonly CaseElseExpr<TReturnExpr> _elseClause = elseClause;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpace(_expr)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
