namespace SqlArtisan.Internal;

public sealed class DatepartFunction : SqlExpression
{
    private readonly DateTimeField _datepart;
    private readonly SqlExpression _source;

    internal DatepartFunction(DateTimeField datepart, SqlExpression source)
    {
        _datepart = datepart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datepart)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .PrependComma(_source)
        .CloseParenthesis();
}
