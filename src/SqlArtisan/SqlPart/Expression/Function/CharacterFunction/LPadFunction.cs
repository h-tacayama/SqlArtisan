namespace SqlArtisan;

public sealed class LPadFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _length;
    private readonly SqlExpression? _padding;

    internal LPadFunction(
        SqlExpression source,
        SqlExpression length,
        SqlExpression? padding = null)
    {
        _source = source;
        _length = length;
        _padding = padding;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LPad)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_length)
        .PrependCommaIfNotNull(_padding)
        .CloseParenthesis();
}
