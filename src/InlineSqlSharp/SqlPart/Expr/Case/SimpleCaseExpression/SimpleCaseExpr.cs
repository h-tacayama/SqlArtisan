namespace InlineSqlSharp;

public sealed class SimpleCaseExpr : AbstractExpr
{
    private readonly AbstractExpr _expr;
    private readonly SimpleCaseWhenClause[] _whenClauses;
    private readonly CaseElseExpr _elseClause;

    internal SimpleCaseExpr(
        AbstractExpr expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr elseClause)
    {
        _expr = expr;
        _whenClauses = whenClauses;
        _elseClause = elseClause;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpace(_expr)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
