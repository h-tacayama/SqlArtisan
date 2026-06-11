namespace SqlArtisan.Internal;

public sealed class AnalyticNthValueFunction : ValueAnalyticFunction
{
    private readonly SqlExpression _expr;
    private readonly int _n;

    internal AnalyticNthValueFunction(SqlExpression expr, int n)
    {
        _expr = expr;
        _n = n;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.NthValue)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_n.ToInvariantString())
        .CloseParenthesis();
}
