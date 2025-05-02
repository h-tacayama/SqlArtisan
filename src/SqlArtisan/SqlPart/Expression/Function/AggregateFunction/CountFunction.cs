namespace SqlArtisan;

public sealed class CountFunction : SqlExpression
{
    private readonly DistinctKeyword? _distinct;
    private readonly SqlPart _expr;

    internal CountFunction(SqlExpression expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal CountFunction(DistinctKeyword distinct, SqlExpression expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Count)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
