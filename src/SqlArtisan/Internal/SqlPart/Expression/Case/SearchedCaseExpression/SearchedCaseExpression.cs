namespace SqlArtisan.Internal;

public sealed class SearchedCaseExpression : SqlExpression
{
    private readonly SearchedCaseWhenClause[] _whenClauses;
    private readonly CaseElseExpression? _elseClause;

    internal SearchedCaseExpression(SearchedCaseWhenClause[] whenClauses)
    {
        _whenClauses = whenClauses;
        _elseClause = null;
    }

    internal SearchedCaseExpression(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpression elseClause)
    {
        _whenClauses = whenClauses;
        _elseClause = elseClause;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Case} ")
            .AppendSpaceSeparated(_whenClauses);

        if (_elseClause is not null)
        {
            buffer.Append($" {Keywords.Else} ")
                .Append(_elseClause);
        }

        buffer.Append($" {Keywords.End}");
    }
}
