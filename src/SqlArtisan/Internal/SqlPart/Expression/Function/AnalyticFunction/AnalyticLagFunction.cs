namespace SqlArtisan.Internal;

public sealed class AnalyticLagFunction : AnalyticFunction
{
    private readonly SqlExpression _expr;
    private readonly int? _offset;
    private readonly SqlExpression? _default;

    internal AnalyticLagFunction(SqlExpression expr)
    {
        _expr = expr;
        _offset = null;
        _default = null;
    }

    internal AnalyticLagFunction(SqlExpression expr, int offset)
    {
        _expr = expr;
        _offset = offset;
        _default = null;
    }

    internal AnalyticLagFunction(
        SqlExpression expr,
        int offset,
        SqlExpression @default)
    {
        _expr = expr;
        _offset = offset;
        _default = @default;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Lag)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_offset?.ToInvariantString())
        .PrependCommaIfNotNull(_default)
        .CloseParenthesis();
}
