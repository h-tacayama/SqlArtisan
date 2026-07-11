namespace SqlArtisan.Internal;

/// <summary>
/// The qualified star select item — <c>"t".*</c> (aliased) or <c>customer.*</c>
/// (bare table name): every column of one table. Select-item-only, like
/// <see cref="AsteriskMarker"/>.
/// </summary>
public sealed class QualifiedAsteriskMarker : SqlPart
{
    private readonly string _qualifier;
    private readonly bool _quoteQualifier;

    internal QualifiedAsteriskMarker(string qualifier, bool quoteQualifier)
    {
        _qualifier = qualifier;
        _quoteQualifier = quoteQualifier;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_quoteQualifier)
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
