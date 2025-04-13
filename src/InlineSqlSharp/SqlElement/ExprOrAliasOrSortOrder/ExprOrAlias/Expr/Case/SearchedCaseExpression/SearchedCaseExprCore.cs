namespace InlineSqlSharp;

internal sealed class SearchedCaseExprCore<TElse>(
    SearchedCaseWhenClause[] whenClauses,
    CaseElseExpr<TElse> elseClause) where TElse : IExpr
{
    private readonly SearchedCaseWhenClause[] _whenClauses = whenClauses;
    private readonly CaseElseExpr<TElse> _elseClause = elseClause;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
