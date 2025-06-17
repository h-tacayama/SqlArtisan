namespace SqlArtisan.Internal;

public sealed class LtrimFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression? _trimChars;

    internal LtrimFunction(
        SqlExpression source,
        SqlExpression? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Ltrim)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
