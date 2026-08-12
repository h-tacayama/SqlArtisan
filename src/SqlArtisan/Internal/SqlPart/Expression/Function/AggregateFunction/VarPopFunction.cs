namespace SqlArtisan.Internal;

public sealed class VarPopFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal VarPopFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.VarPop)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
