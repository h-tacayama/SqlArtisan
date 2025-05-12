namespace SqlArtisan;

public sealed class ToTimestampFunction : SqlExpression
{
    private readonly SqlExpression _text;
    private readonly SqlExpression _format;

    internal ToTimestampFunction(SqlExpression text, SqlExpression format)
    {
        _text = text;
        _format = format;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ToTimestamp)
        .OpenParenthesis()
        .Append(_text)
        .PrependComma(_format)
        .CloseParenthesis();
}
