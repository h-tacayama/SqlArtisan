namespace SqlArtisan.Internal;

public sealed class TimestampdiffFunction : SqlExpression
{
    private readonly DateTimePart _unit;
    private readonly SqlExpression _startDate;
    private readonly SqlExpression _endDate;

    internal TimestampdiffFunction(DateTimePart unit, SqlExpression startDate, SqlExpression endDate)
    {
        _unit = unit;
        _startDate = startDate;
        _endDate = endDate;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Timestampdiff)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_unit))
        .PrependComma(_startDate)
        .PrependComma(_endDate)
        .CloseParenthesis();
}
