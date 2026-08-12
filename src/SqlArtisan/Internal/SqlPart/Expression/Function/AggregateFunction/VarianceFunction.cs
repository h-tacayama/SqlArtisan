namespace SqlArtisan.Internal;

public sealed class VarianceFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal VarianceFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Variance)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
