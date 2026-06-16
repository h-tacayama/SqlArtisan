namespace SqlArtisan.Internal;

public sealed class ExtractFunction : SqlExpression
{
    private readonly DateTimeField _datepart;
    private readonly SqlExpression _source;

    internal ExtractFunction(DateTimeField datepart, SqlExpression source)
    {
        _datepart = datepart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Extract)
        .OpenParenthesis()
        .Append(DatepartKeywords.Of(_datepart))
        .EncloseInSpaces(Keywords.From)
        .Append(_source)
        .CloseParenthesis();
}
