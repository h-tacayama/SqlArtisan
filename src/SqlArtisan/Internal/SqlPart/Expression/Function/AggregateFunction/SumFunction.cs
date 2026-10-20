namespace SqlArtisan.Internal;

internal sealed class SumFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal SumFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Sum)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
