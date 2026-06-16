namespace SqlArtisan.Internal;

public sealed class DatediffFunction : SqlExpression
{
    private readonly DateTimeField _datepart;
    private readonly SqlExpression _startDate;
    private readonly SqlExpression _endDate;

    internal DatediffFunction(
        DateTimeField datepart,
        SqlExpression startDate,
        SqlExpression endDate)
    {
        _datepart = datepart;
        _startDate = startDate;
        _endDate = endDate;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datediff)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_startDate)
        .PrependComma(_endDate)
        .CloseParenthesis();
}
