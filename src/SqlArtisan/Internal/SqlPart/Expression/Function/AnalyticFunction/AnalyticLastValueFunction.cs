namespace SqlArtisan.Internal;

public sealed class AnalyticLastValueFunction : ValueAnalyticFunction
{
    private readonly SqlExpression _expr;

    internal AnalyticLastValueFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LastValue)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
