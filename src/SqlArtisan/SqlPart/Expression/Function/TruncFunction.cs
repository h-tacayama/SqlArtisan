namespace SqlArtisan;

public sealed class TruncFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression? _format;

    internal TruncFunction(SqlExpression expr, SqlExpression? format = null)
    {
        _expr = expr;
        _format = format;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Trunc)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_format)
        .CloseParenthesis();
}
