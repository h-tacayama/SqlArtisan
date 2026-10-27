namespace SqlArtisan.Internal;

/// <summary>
/// The qualified star select item — <c>"t".*</c> (aliased) or <c>customer.*</c>
/// (bare table name): every column of one table.
/// </summary>
internal sealed class QualifiedAsteriskMarker : SqlPart
{
    private readonly string _qualifier;
    private readonly bool _quoteQualifier;
    private readonly int _nameStart;

    internal QualifiedAsteriskMarker(string correlationName)
    {
        _qualifier = correlationName;
        _quoteQualifier = true;
    }

    internal QualifiedAsteriskMarker(string tableName, int nameStart)
    {
        _qualifier = tableName;
        _nameStart = nameStart;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_quoteQualifier)
        {
            buffer.EncloseInAliasQuotes(_qualifier);
        }
        else if (buffer.QualifiedAsteriskDropsSchema)
        {
            buffer.Append(_qualifier.AsSpan(_nameStart));
        }
        else
        {
            buffer.Append(_qualifier);
        }

        buffer.Append('.');
        buffer.Append(Operators.Asterisk);
    }
}
