namespace SqlArtisan.Internal;

internal sealed class DatepartFunction : SqlExpression
{
    private readonly string _datepart;
    private readonly SqlExpression _source;

    internal DatepartFunction(DateTimePart datepart, SqlExpression source)
    {
        _datepart = DatepartKeywords.Resolve(datepart, Keywords.Datepart, nameof(datepart));
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datepart)
        .OpenParenthesis()
        .Append(_datepart)
        .PrependComma(_source)
        .CloseParenthesis();
}
