namespace SqlArtisan.Internal;

public sealed class TimestampdiffFunction : SqlExpression
{
    private readonly string _unit;
    private readonly SqlExpression _startDate;
    private readonly SqlExpression _endDate;

    internal TimestampdiffFunction(
        DateTimePart unit,
        SqlExpression startDate,
        SqlExpression endDate)
    {
        _unit = DatepartKeywords.Resolve(unit, Keywords.Timestampdiff, nameof(unit));
        _startDate = startDate;
        _endDate = endDate;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Timestampdiff)
        .OpenParenthesis()
        .Append(_unit)
        .PrependComma(_startDate)
        .PrependComma(_endDate)
        .CloseParenthesis();
}
