namespace SqlArtisan.Internal;

public sealed class DateAddFunction : SqlExpression
{
    private readonly Datepart _datepart;
    private readonly SqlExpression _number;
    private readonly SqlExpression _dateTime;

    internal DateAddFunction(
        Datepart datepart,
        SqlExpression number,
        SqlExpression dateTime)
    {
        _datepart = datepart;
        _number = number;
        _dateTime = dateTime;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateAdd)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_number)
        .PrependComma(_dateTime)
        .CloseParenthesis();
}
