namespace SqlArtisan.Internal;

public sealed class VarFunction : UnfilteredAggregateFunction
{
    private readonly SqlPart _expr;

    internal VarFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Var)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
