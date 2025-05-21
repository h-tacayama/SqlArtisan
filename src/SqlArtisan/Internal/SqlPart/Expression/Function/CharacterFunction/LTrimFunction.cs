namespace SqlArtisan.Internal;

public sealed class LTrimFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression? _trimChars;

    internal LTrimFunction(
        SqlExpression source,
        SqlExpression? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LTrim)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
