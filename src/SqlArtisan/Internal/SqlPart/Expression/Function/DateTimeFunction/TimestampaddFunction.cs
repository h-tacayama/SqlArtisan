namespace SqlArtisan.Internal;

public sealed class TimestampaddFunction : SqlExpression
{
    private readonly DateTimePart _unit;
    private readonly SqlExpression _number;
    private readonly SqlExpression _dateTime;

    internal TimestampaddFunction(DateTimePart unit, SqlExpression number, SqlExpression dateTime)
    {
        _unit = unit;
        _number = number;
        _dateTime = dateTime;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Timestampadd)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_unit))
        .PrependComma(_number)
        .PrependComma(_dateTime)
        .CloseParenthesis();
}
