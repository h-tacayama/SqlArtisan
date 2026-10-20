namespace SqlArtisan.Internal;

internal sealed class DatediffFunction : SqlExpression
{
    private readonly string _datepart;
    private readonly SqlExpression _startDate;
    private readonly SqlExpression _endDate;

    internal DatediffFunction(
        DateTimePart datepart,
        SqlExpression startDate,
        SqlExpression endDate)
    {
        _datepart = DatepartKeywords.Resolve(datepart, Keywords.Datediff, nameof(datepart));
        _startDate = startDate;
        _endDate = endDate;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datediff)
        .OpenParenthesis()
        .Append(_datepart)
        .PrependComma(_startDate)
        .PrependComma(_endDate)
        .CloseParenthesis();
}
