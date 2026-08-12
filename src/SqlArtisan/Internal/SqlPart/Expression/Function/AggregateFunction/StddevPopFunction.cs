namespace SqlArtisan.Internal;

public sealed class StddevPopFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal StddevPopFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.StddevPop)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
