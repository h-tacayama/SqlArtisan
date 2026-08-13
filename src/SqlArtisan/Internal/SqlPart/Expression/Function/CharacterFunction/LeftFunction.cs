namespace SqlArtisan.Internal;

public sealed class LeftFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _length;

    internal LeftFunction(SqlExpression source, SqlExpression length)
    {
        _source = source;
        _length = length;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Left)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_length)
        .CloseParenthesis();
}
