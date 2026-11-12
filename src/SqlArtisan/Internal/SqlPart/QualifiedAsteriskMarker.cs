namespace SqlArtisan.Internal;

/// <summary>
/// The qualified star select item — <c>"t".*</c> (aliased) or <c>customer.*</c>
/// (bare table name): every column of one table.
/// </summary>
internal sealed class QualifiedAsteriskMarker : SqlPart
{
    private readonly string _qualifier;
    private readonly bool _isQualifierQuoted;

    internal QualifiedAsteriskMarker(string qualifier, bool quoteQualifier)
    {
        _qualifier = qualifier;
        _isQualifierQuoted = quoteQualifier;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_isQualifierQuoted)
        {
            buffer.EncloseInAliasQuotes(_qualifier);
        }
        else
        {
            buffer.Append(_qualifier);
        }

        buffer.Append('.');
        buffer.Append(Operators.Asterisk);
    }
}
