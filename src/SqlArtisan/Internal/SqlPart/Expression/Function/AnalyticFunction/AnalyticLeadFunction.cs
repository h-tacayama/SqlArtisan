namespace SqlArtisan.Internal;

public sealed class AnalyticLeadFunction : AnalyticFunction
{
    private readonly SqlExpression _expr;
    private readonly int? _offset;
    private readonly SqlExpression? _default;

    internal AnalyticLeadFunction(SqlExpression expr)
    {
        _expr = expr;
        _offset = null;
        _default = null;
    }

    internal AnalyticLeadFunction(SqlExpression expr, int offset)
    {
        _expr = expr;
        _offset = offset;
        _default = null;
    }

    internal AnalyticLeadFunction(SqlExpression expr, int offset, SqlExpression @default)
    {
        _expr = expr;
        _offset = offset;
        _default = @default;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Lead)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_offset?.ToInvariantString())
        .PrependCommaIfNotNull(_default)
        .CloseParenthesis();
}
