namespace SqlArtisan.Internal;

public sealed class ExtractFunction : SqlExpression
{
    private readonly Datepart _datepart;
    private readonly SqlExpression _source;

    internal ExtractFunction(Datepart datePart, SqlExpression source)
    {
        _datepart = datePart;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Extract)
        .OpenParenthesis()
        .AppendUpperSnakeCase(_datepart)
        .EncloseInSpaces(Keywords.From)
        .Append(_source)
        .CloseParenthesis();
}
