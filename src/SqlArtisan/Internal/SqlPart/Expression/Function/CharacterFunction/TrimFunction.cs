namespace SqlArtisan.Internal;

internal sealed class TrimFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression? _trimChar;

    internal TrimFunction(SqlExpression source, SqlExpression? trimChar = null)
    {
        _source = source;
        _trimChar = trimChar;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Trim).OpenParenthesis();

        if (_trimChar is not null)
        {
            buffer.Append(_trimChar).EncloseInSpaces(Keywords.From);
        }

        buffer.Append(_source).CloseParenthesis();
    }
}
