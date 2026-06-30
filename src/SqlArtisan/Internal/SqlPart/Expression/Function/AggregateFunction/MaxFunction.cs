namespace SqlArtisan.Internal;

public sealed class MaxFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal MaxFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Max)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
