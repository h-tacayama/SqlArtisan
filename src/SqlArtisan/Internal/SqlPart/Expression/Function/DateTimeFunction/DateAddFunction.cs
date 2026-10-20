namespace SqlArtisan.Internal;

internal sealed class DateAddFunction : SqlExpression
{
    private readonly SqlExpression _date;
    private readonly SqlPart _interval;

    internal DateAddFunction(SqlExpression date, SqlPart interval)
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
