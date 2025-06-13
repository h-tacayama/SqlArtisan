namespace SqlArtisan.Internal;

public sealed class ExtractFunction : SqlExpression
{
    private readonly DatePart _datePart;
    private readonly SqlExpression _source;

    internal ExtractFunction(DatePart datePart, SqlExpression source)
    {
        _datePart = datePart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Extract)
        .OpenParenthesis()
        .Append(_datePart.ToSql())
        .EncloseInSpaces(Keywords.From)
        .Append(_source)
        .CloseParenthesis();
}
