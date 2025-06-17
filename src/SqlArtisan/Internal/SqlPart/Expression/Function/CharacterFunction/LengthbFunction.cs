namespace SqlArtisan.Internal;

public sealed class LengthbFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal LengthbFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Lengthb)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
