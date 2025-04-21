namespace InlineSqlSharp;

public sealed class SimpleCaseExpr(
    AbstractExpr expr,
    SimpleCaseWhenClause[] whenClauses,
    CaseElseExpr elseClause) : AbstractExpr
{
    private readonly AbstractExpr _expr = expr;
    private readonly SimpleCaseWhenClause[] _whenClauses = whenClauses;
    private readonly CaseElseExpr _elseClause = elseClause;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpace(_expr)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
