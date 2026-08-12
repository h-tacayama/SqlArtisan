namespace SqlArtisan.Internal;

public sealed class VarSampFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal VarSampFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.VarSamp)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
