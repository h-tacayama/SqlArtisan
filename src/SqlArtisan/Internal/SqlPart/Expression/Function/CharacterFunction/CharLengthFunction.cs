namespace SqlArtisan.Internal;

public sealed class CharLengthFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal CharLengthFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.CharLength)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
