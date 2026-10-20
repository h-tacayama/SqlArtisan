namespace SqlArtisan.Internal;

internal sealed class CountFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal CountFunction(AsteriskMarker asterisk)
    {
        _expr = asterisk;
    }

    internal CountFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Count)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
