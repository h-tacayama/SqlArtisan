namespace SqlArtisan.Internal;

public sealed class DateTruncFunction : SqlExpression
{
    private readonly DateTimePart _datepart;
    private readonly SqlExpression _source;

    internal DateTruncFunction(DateTimePart datepart, SqlExpression source)
    {
        _datepart = datepart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateTrunc)
        .OpenParenthesis()
        .Append('\'')
        .Append(DatepartKeywords.Of(_datepart))
        .Append('\'')
        .PrependComma(_source)
        .CloseParenthesis();
}
