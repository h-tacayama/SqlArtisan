namespace InlineSqlSharp;

internal sealed class SimpleCaseExprCore<TElse>(
    object expr,
    SimpleCaseWhenClause[] whenClauses,
    CaseElseExpr<TElse> elseClause)
    where TElse : IExpr
{
    private readonly object _expr = expr;
    private readonly SimpleCaseWhenClause[] _whenClauses = whenClauses;
    private readonly CaseElseExpr<TElse> _elseClause = elseClause;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpace(_expr)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
