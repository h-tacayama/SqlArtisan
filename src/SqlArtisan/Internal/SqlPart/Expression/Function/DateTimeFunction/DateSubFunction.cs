namespace SqlArtisan.Internal;

public sealed class DateSubFunction : SqlExpression
{
    private readonly SqlExpression _date;
    private readonly SqlPart _interval;

    internal DateSubFunction(SqlExpression date, SqlPart interval)
    {
        _date = date;
        _interval = interval;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateSub)
        .OpenParenthesis()
        .Append(_date)
        .PrependComma(_interval)
        .CloseParenthesis();
}
