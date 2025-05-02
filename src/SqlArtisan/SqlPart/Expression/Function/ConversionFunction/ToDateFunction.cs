namespace SqlArtisan;

public sealed class ToDateFunction : SqlExpression
{
    private readonly SqlExpression _text;
    private readonly SqlExpression _format;

    internal ToDateFunction(SqlExpression text, SqlExpression format)
    {
        _text = text;
        _format = format;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ToDate)
        .OpenParenthesis()
        .Append(_text)
        .PrependComma(_format)
        .CloseParenthesis();
}
