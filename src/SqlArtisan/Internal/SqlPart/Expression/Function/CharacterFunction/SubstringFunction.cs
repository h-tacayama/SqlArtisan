namespace SqlArtisan.Internal;

public sealed class SubstringFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _position;
    private readonly SqlExpression _length;

    internal SubstringFunction(
        SqlExpression source,
        SqlExpression position,
        SqlExpression length)
    {
        _source = source;
        _position = position;
        _length = length;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Substring)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_position)
        .PrependComma(_length)
        .CloseParenthesis();
}
