namespace SqlArtisan.Internal;

public sealed class DateTruncFunction : SqlExpression
{
    private readonly string _datepart;
    private readonly SqlExpression _source;

    internal DateTruncFunction(DateTimePart datepart, SqlExpression source)
    {
        _datepart = DatepartKeywords.ResolveLower(datepart, Keywords.DateTrunc, nameof(datepart));
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DateTrunc)
        .OpenParenthesis()
        .AppendStringLiteral(_datepart)
        .PrependComma(_source)
        .CloseParenthesis();
}
