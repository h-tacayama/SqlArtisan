namespace SqlArtisan;

public sealed class SearchedCaseExpression : SqlExpression
{
    private readonly SearchedCaseWhenClause[] _whenClauses;
    private readonly CaseElseExpression _elseClause;

    internal SearchedCaseExpression(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpression elseClause)
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
