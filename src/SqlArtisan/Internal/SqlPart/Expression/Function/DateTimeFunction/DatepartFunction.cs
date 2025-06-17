namespace SqlArtisan.Internal;

public sealed class DatepartFunction : SqlExpression
{
    private readonly Datepart _datepart;
    private readonly SqlExpression _source;

    internal DatepartFunction(Datepart datePart, SqlExpression source)
    {
        _datepart = datePart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Datepart)
        .OpenParenthesis()
        .AppendUpperSnakeCase(_datepart)
        .PrependComma(_source)
        .CloseParenthesis();
}
