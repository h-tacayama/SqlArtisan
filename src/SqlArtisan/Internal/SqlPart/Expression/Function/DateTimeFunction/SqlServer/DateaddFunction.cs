namespace SqlArtisan.Internal;

public sealed class DateaddFunction : SqlExpression
{
    private readonly DateTimePart _datepart;
    private readonly SqlExpression _number;
    private readonly SqlExpression _dateTime;

    internal DateaddFunction(DateTimePart datepart, SqlExpression number, SqlExpression dateTime)
    {
        _datepart = datepart;
        _number = number;
        _dateTime = dateTime;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Dateadd)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_number)
        .PrependComma(_dateTime)
        .CloseParenthesis();
}
