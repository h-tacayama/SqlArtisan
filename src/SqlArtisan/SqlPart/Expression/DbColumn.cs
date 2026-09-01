using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A reference to a table column, rendered <c>alias.column</c> (or bare <c>column</c>
/// when the owning table has no correlation name). Expose one per column from a
/// <see cref="DbTableBase"/> subclass.
/// </summary>
public sealed class DbColumn : SqlExpression
{
    private readonly bool _quoteName;

    /// <summary>
    /// Creates a reference to the named column of <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The table, CTE, or derived table that owns this column.</param>
    /// <param name="name">The column name as it appears in SQL.</param>
    public DbColumn(TableReference owner, string name)
    {
        ArgumentNullException.ThrowIfNull(owner);
        StringGuard.ThrowIfNullOrEmpty(name, "A column requires a name.");

        Owner = owner;
        Name = name;
    }

    // For a column materialized from a quoted SELECT-list alias: the reference
    // must render quoted exactly as the definition did, or a case-folding
    // engine resolves the two to different identifiers.
    internal DbColumn(TableReference owner, string name, bool quoteName)
        : this(owner, name)
    {
        _quoteName = quoteName;
    }

    internal TableReference Owner { get; }
    internal string Name { get; }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        string correlationName = Owner.CorrelationName;

        if (!string.IsNullOrEmpty(correlationName))
        {
            buffer.EncloseInAliasQuotes(correlationName);
            buffer.Append('.');
        }
        else
        {
            // A bare DML-target column inside a subquery resolves to the inner
            // scope — a silent tautology — so the guard fails loudly (#253).
            buffer.ThrowIfCorrelatedDmlColumn(Owner);
        }

        AppendName(buffer);
    }

    // Renders the bare column name: a DML context that names a target column
    // must stay unqualified — PostgreSQL rejects an alias qualifier there.
    internal void FormatUnqualified(SqlBuildingBuffer buffer) =>
        AppendName(buffer);

    private void AppendName(SqlBuildingBuffer buffer)
    {
        if (_quoteName)
        {
            buffer.EncloseInAliasQuotes(Name);
        }
        else
        {
            buffer.Append(Name);
        }
    }
}
