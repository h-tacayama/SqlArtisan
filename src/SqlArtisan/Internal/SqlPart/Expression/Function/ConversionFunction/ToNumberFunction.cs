namespace SqlArtisan.Internal;

public sealed class ToNumberFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression? _format;

    internal ToNumberFunction(
        SqlExpression expr,
        SqlExpression? format = null)
    {
        _expr = expr;
        _format = format;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ToNumber)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_format)
        .CloseParenthesis();
}
