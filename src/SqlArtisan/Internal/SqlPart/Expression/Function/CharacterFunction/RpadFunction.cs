namespace SqlArtisan.Internal;

public sealed class RpadFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _length;
    private readonly SqlExpression? _padding;

    internal RpadFunction(
        SqlExpression source,
        SqlExpression length,
        SqlExpression? padding = null)
    {
        _source = source;
        _length = length;
        _padding = padding;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Rpad)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_length)
        .PrependCommaIfNotNull(_padding)
        .CloseParenthesis();
}
