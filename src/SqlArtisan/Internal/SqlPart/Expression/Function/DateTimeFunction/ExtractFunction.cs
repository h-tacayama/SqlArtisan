namespace SqlArtisan.Internal;

internal sealed class ExtractFunction : SqlExpression
{
    private readonly string _datepart;
    private readonly SqlExpression _source;

    internal ExtractFunction(DateTimePart datepart, SqlExpression source)
    {
        _datepart = DatepartKeywords.Resolve(datepart, Keywords.Extract, nameof(datepart));
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Extract)
        .OpenParenthesis()
        .Append(_datepart)
        .EncloseInSpaces(Keywords.From)
        .Append(_source)
        .CloseParenthesis();
}
