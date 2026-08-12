namespace SqlArtisan.Internal;

public sealed class StdevpFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal StdevpFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Stdevp)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
