namespace SqlArtisan.Internal;

public sealed class SubstrbFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _position;
    private readonly SqlExpression? _length;

    internal SubstrbFunction(
        SqlExpression source,
        SqlExpression position,
        SqlExpression? length = null)
    {
        _source = source;
        _position = position;
        _length = length;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Substrb)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_position)
        .PrependCommaIfNotNull(_length)
        .CloseParenthesis();
}
