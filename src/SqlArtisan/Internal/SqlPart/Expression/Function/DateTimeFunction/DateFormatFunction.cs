namespace SqlArtisan.Internal;

public sealed class DateFormatFunction : SqlExpression
{
    private readonly SqlExpression _date;
    private readonly SqlExpression _format;

    internal DateFormatFunction(SqlExpression date, SqlExpression format)
    {
        _date = date;
        _format = format;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateFormat)
        .OpenParenthesis()
        .Append(_date)
        .PrependComma(_format)
        .CloseParenthesis();
}
