namespace SqlArtisan.Internal;

public sealed class LengthFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal LengthFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Length)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
