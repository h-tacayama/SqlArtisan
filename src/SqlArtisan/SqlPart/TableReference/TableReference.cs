using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A named relation usable in <c>FROM</c>, a <c>JOIN</c>, or MERGE's
/// <c>USING</c> — the base type of <see cref="DbTableBase"/>, <see cref="CteBase"/>,
/// <see cref="DerivedTableBase"/>, and <see cref="DualTable"/>. Type a collection or
/// helper as this to work across all four.
/// </summary>
public abstract class TableReference : SqlPart
{
    private protected readonly string _name;

    /// <summary>
    /// Names the relation explicitly.
    /// </summary>
    /// <param name="name">The relation name as it appears in SQL.</param>
    /// <param name="emptyNameMessage">
    /// The message to throw with when <paramref name="name"/> is null or
    /// empty, worded for the calling subclass's construct.
    /// </param>
    private protected TableReference(string name, string emptyNameMessage)
    {
        StringGuard.ThrowIfNullOrEmpty(name, emptyNameMessage);
        _name = name;
    }

    /// <summary>
    /// The qualified star select item — <c>"alias".*</c> (a CTE/derived-table name
    /// is always quoted), or <c>table.*</c> for an unaliased <see cref="DbTableBase"/>.
    /// Valid only in a <c>SELECT</c>, <c>RETURNING</c>, or <c>OUTPUT</c> list.
    /// </summary>
    public QualifiedAsteriskMarker Asterisk =>
        string.IsNullOrEmpty(CorrelationName)
            ? new(_name, quoteQualifier: false)
            : new(CorrelationName, quoteQualifier: true);

    internal abstract string CorrelationName { get; }

    // The name a qualifier in an enclosing scope resolves against: the
    // correlation name, or else the name without its schema.
    internal ReadOnlySpan<char> ExposedName =>
        string.IsNullOrEmpty(CorrelationName) ? NameWithoutSchema : CorrelationName;

    // A name ending in its separator leaves nothing to strip down to, so it
    // stays whole, as FROM renders it, rather than becoming empty.
    internal ReadOnlySpan<char> NameWithoutSchema
    {
        get
        {
            int start = LastQualifierEnd(_name);
            return start == 0 || start == _name.Length ? _name : _name.AsSpan(start);
        }
    }

    // A CTE or derived-table name also qualifies column references, so it is quoted
    // to match them: a bare name case-folds on Oracle while the quoted reference does
    // not (ORA-00904).
    private protected virtual bool QuoteName => false;

    // The index just past the last `.` outside a quoted identifier, or 0 when
    // the name is unqualified.
    private static int LastQualifierEnd(string name)
    {
        int end = 0;
        char quote = '\0';
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }
            }
            else if (c is '"' or '`')
            {
                quote = c;
            }
            else if (c == '[')
            {
                quote = ']';
            }
            else if (c == '.')
            {
                end = i + 1;
            }
        }

        return end;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (QuoteName)
        {
            buffer.EncloseInAliasQuotes(_name);
        }
        else
        {
            buffer.Append(_name);
        }
    }
}
