namespace SqlArtisan.Internal;

public sealed class FormatFunction : SqlExpression
{
    private readonly SqlExpression _value;
    private readonly SqlExpression _format;
    private readonly SqlExpression? _culture;

    internal FormatFunction(SqlExpression value, SqlExpression format, SqlExpression? culture = null)
    {
        _value = value;
        _format = format;
        _culture = culture;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Format)
        .OpenParenthesis()
        .Append(_value)
        .PrependComma(_format)
        .PrependCommaIfNotNull(_culture)
        .CloseParenthesis();
}
