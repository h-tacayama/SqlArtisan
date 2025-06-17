namespace SqlArtisan.Internal;

public sealed class RtrimFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression? _trimChars;

    internal RtrimFunction(
        SqlExpression source,
        SqlExpression? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Rtrim)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
