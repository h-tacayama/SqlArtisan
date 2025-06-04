namespace SqlArtisan.Internal;

public sealed class SimpleCaseExpression : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SimpleCaseWhenClause[] _whenClauses;
    private readonly CaseElseExpression? _elseClause;

    internal SimpleCaseExpression(
        SqlExpression expr,
        SimpleCaseWhenClause[] whenClauses)
    {
        _expr = expr;
        _whenClauses = whenClauses;
        _elseClause = null;
    }

    internal SimpleCaseExpression(
        SqlExpression expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpression elseClause)
    {
        _expr = expr;
        _whenClauses = whenClauses;
        _elseClause = elseClause;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Case} ")
            .AppendSpace(_expr)
            .AppendSpaceSeparated(_whenClauses);

        if (_elseClause is not null)
        {
            buffer.Append($" {Keywords.Else} ")
                .Append(_elseClause);
        }

        buffer.Append($" {Keywords.End}");
    }
}
