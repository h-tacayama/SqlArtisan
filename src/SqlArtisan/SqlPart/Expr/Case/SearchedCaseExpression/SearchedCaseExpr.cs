namespace SqlArtisan;

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
        .Append($"{Keywords.Case} ")
        .AppendSpaceSeparated(_whenClauses)
        .Append($" {Keywords.Else} ")
        .Append(_elseClause)
        .Append($" {Keywords.End}");
}
