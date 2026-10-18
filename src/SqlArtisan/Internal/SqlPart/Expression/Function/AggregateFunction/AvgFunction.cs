namespace SqlArtisan.Internal;

public sealed class AvgFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal AvgFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Avg)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
