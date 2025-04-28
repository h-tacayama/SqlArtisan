namespace InlineSqlSharp;

public sealed class SearchedCaseExpr : AbstractExpr
{
    private readonly SearchedCaseWhenClause[] _whenClauses;
    private readonly CaseElseExpr _elseClause;

    internal SearchedCaseExpr(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr elseClause)
    {
        _whenClauses = whenClauses;
        _elseClause = elseClause;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Case)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.Else)
        .AppendSpace(_elseClause)
        .Append(Keywords.End);
}
