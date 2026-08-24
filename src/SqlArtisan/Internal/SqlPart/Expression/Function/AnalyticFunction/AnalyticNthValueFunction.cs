namespace SqlArtisan.Internal;

public sealed class AnalyticNthValueFunction : ValueAnalyticFunction
{
    private readonly SqlExpression _expr;

    // Stringified once at construction, not per Format (ADR 0006).
    private readonly string _n;

    internal AnalyticNthValueFunction(SqlExpression expr, int n)
    {
        _expr = expr;
        _n = WindowFrameGuard.ValidateNthValuePosition(n).ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.NthValue)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_n)
        .CloseParenthesis();
}
