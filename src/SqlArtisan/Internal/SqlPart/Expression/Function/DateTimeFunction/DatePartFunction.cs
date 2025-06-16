namespace SqlArtisan.Internal;

public sealed class DatePartFunction : SqlExpression
{
    private readonly DatePart _datePart;
    private readonly SqlExpression _source;

    internal DatePartFunction(DatePart datePart, SqlExpression source)
    {
        _datePart = datePart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DatePart)
        .OpenParenthesis()
        .AppendUpperSnakeCase(_datePart)
        .PrependComma(_source)
        .CloseParenthesis();
}
