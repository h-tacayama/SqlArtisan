namespace SqlArtisan;

public sealed class RTrimFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression? _trimChars;

    internal RTrimFunction(
        SqlExpression source,
        SqlExpression? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RTrim)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
