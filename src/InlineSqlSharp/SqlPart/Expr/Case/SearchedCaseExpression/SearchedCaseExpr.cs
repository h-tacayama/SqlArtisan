namespace InlineSqlSharp;

public sealed class SearchedCaseExpr(
    SearchedCaseWhenClause[] whenClauses,
    CaseElseExpr elseClause) : AbstractExpr
{
    private readonly SearchedCaseWhenClause[] _whenClauses = whenClauses;
    private readonly CaseElseExpr _elseClause = elseClause;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.CASE)
        .AppendSpaceSeparated(_whenClauses)
        .AppendSpace()
        .AppendSpace(Keywords.ELSE)
        .AppendSpace(_elseClause)
        .Append(Keywords.END);
}
