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

    // Each subclass names its own construct in the empty-name message.
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

    // The name used to qualify column references belonging to this relation.
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

    // Whether the name is alias-quoted when rendered. A reference whose name also
    // qualifies column references — a CTE or derived table — must quote it so the
    // two agree; otherwise a bare name case-folds on Oracle (`x` -> `X`) while the
    // quoted column reference does not, breaking resolution (ORA-00904). Real table
    // names stay bare (DbTableBase quotes only its separate alias). Overriding this
    // flag is the single place a subclass opts into quoting.
    private protected virtual bool QuoteName => false;

    // The index just past the last `.` outside a quoted identifier, or 0 when
    // the name is unqualified: `public.users` -> `users`, `"a.b"` stays whole.
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
