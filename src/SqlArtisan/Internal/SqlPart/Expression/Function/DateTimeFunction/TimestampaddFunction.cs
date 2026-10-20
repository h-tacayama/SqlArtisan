namespace SqlArtisan.Internal;

internal sealed class TimestampaddFunction : SqlExpression
{
    private readonly string _unit;
    private readonly SqlExpression _number;
    private readonly SqlExpression _dateTime;

    internal TimestampaddFunction(DateTimePart unit, SqlExpression number, SqlExpression dateTime)
    {
        _unit = DatepartKeywords.Resolve(unit, Keywords.Timestampadd, nameof(unit));
        _number = number;
        _dateTime = dateTime;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Timestampadd)
        .OpenParenthesis()
        .Append(_unit)
        .PrependComma(_number)
        .PrependComma(_dateTime)
        .CloseParenthesis();
}
