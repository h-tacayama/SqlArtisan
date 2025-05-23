namespace SqlArtisan.Internal;

public sealed class LengthBFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal LengthBFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LengthB)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
