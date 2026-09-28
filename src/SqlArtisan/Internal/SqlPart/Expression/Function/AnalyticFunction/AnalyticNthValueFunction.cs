namespace SqlArtisan.Internal;

public sealed class AnalyticNthValueFunction : ValueAnalyticFunction
{
    private readonly SqlExpression _expr;

    private readonly string _n;

    internal AnalyticNthValueFunction(SqlExpression expr, int n)
    {
        _expr = expr;
        _n = WindowFrameGuard.ValidateNthValuePosition(n).ToInvariantString();
    }

    private protected override string FunctionName => Keywords.NthValue;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.NthValue)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_n)
        .CloseParenthesis();
}
