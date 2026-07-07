namespace SqlArtisan.Internal;

// Nested under SqlServer/ (not alongside the DateTimeFunction category files
// directly) because its filename differs from the sibling DateTruncFunction.cs
// (PostgreSQL's DATE_TRUNC) only by case, which the .NET SDK rejects as a
// duplicate compile item on case-insensitive filesystems (Windows/macOS) —
// see .editorconfig's CA1708 override for the resulting warning.
public sealed class DatetruncFunction : SqlExpression
{
    private readonly DateTimePart _datepart;
    private readonly SqlExpression _date;

    internal DatetruncFunction(DateTimePart datepart, SqlExpression date)
    {
        _datepart = datepart;
        _date = date;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datetrunc)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_date)
        .CloseParenthesis();
}
