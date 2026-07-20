using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A named relation usable in <c>FROM</c>, a <c>JOIN</c>, or MERGE's
/// <c>USING</c> — the base type of <see cref="DbTableBase"/>,
/// <see cref="CteBase"/>, and <see cref="DerivedTableBase"/>. Type a
/// collection or helper as this to work across all three.
/// </summary>
public abstract class TableReference : SqlPart
{
    private protected readonly string _name;

    /// <summary>
    /// Names the relation explicitly.
    /// </summary>
    /// <param name="name">The relation name as it appears in SQL.</param>
    private protected TableReference(string name)
    {
        _name = name;
    }

    /// <summary>
    /// The qualified star select item — <c>"alias".*</c> (a CTE/derived-table name
    /// is always quoted), or <c>table.*</c> for an unaliased <see cref="DbTableBase"/>.
    /// Valid only in a <c>SELECT</c> or <c>RETURNING</c> list.
    /// </summary>
    public QualifiedAsteriskMarker Asterisk =>
        string.IsNullOrEmpty(CorrelationName)
            ? new(_name, quoteQualifier: false)
            : new(CorrelationName, quoteQualifier: true);

    // The name used to qualify column references belonging to this relation.
    internal abstract string CorrelationName { get; }

    // Whether the name is alias-quoted when rendered. A reference whose name also
    // qualifies column references — a CTE or derived table — must quote it so the
    // two agree; otherwise a bare name case-folds on Oracle (`x` -> `X`) while the
    // quoted column reference does not, breaking resolution (ORA-00904). Real table
    // names stay bare (DbTableBase quotes only its separate alias). Overriding this
    // flag is the single place a subclass opts into quoting.
    private protected virtual bool QuoteName => false;

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
