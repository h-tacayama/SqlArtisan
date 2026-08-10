namespace SqlArtisan.Internal;

public sealed class DateAddFunction : SqlExpression
{
    private readonly SqlExpression _date;
    private readonly SqlExpression _interval;

    internal DateAddFunction(SqlExpression date, SqlExpression interval)
    {
        _date = date;
        _interval = interval;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateAdd)
        .OpenParenthesis()
        .Append(_date)
        .PrependComma(_interval)
        .CloseParenthesis();
}
