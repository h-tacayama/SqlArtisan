namespace SqlArtisan.Internal;

// Under SqlServer/ for the case-only filename collision DatetruncFunction.cs
// explains (DateAddFunction.cs is MySQL's DATE_ADD).
internal sealed class DateaddFunction : SqlExpression
{
    private readonly string _datepart;
    private readonly SqlExpression _number;
    private readonly SqlExpression _dateTime;

    internal DateaddFunction(DateTimePart datepart, SqlExpression number, SqlExpression dateTime)
    {
        _datepart = DatepartKeywords.Resolve(datepart, Keywords.Dateadd, nameof(datepart));
        _number = number;
        _dateTime = dateTime;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Dateadd)
        .OpenParenthesis()
        .Append(_datepart)
        .PrependComma(_number)
        .PrependComma(_dateTime)
        .CloseParenthesis();
}
