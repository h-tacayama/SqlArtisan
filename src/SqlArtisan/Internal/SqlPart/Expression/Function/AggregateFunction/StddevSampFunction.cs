namespace SqlArtisan.Internal;

public sealed class StddevSampFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal StddevSampFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.StddevSamp)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
