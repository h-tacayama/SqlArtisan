namespace SqlArtisan.Internal;

public sealed class VarpFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal VarpFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Varp)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
