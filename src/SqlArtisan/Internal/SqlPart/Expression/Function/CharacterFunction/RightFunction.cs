namespace SqlArtisan.Internal;

public sealed class RightFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _length;

    internal RightFunction(SqlExpression source, SqlExpression length)
    {
        _source = source;
        _length = length;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Right)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_length)
        .CloseParenthesis();
}
