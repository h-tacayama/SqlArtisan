namespace SqlArtisan.Internal;

public sealed class AnalyticFirstValueFunction : ValueAnalyticFunction
{
    private readonly SqlExpression _expr;

    internal AnalyticFirstValueFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.FirstValue)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
