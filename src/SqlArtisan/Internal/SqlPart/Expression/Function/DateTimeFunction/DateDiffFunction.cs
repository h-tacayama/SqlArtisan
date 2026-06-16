namespace SqlArtisan.Internal;

public sealed class DateDiffFunction : SqlExpression
{
    private readonly Datepart _datepart;
    private readonly SqlExpression _startDate;
    private readonly SqlExpression _endDate;

    internal DateDiffFunction(
        Datepart datepart,
        SqlExpression startDate,
        SqlExpression endDate)
    {
        _datepart = datepart;
        _startDate = startDate;
        _endDate = endDate;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateDiff)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_startDate)
        .PrependComma(_endDate)
        .CloseParenthesis();
}
