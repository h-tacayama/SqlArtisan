using System.Diagnostics;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An aliased <c>SELECT</c>-list item — <c>expr "alias"</c> — produced by
/// <c>.As(...)</c> on an expression or subquery. Usable as a select item and
/// as an <c>ORDER BY</c> key (by its alias). Type a helper as this to return
/// a named computed column.
/// </summary>
public sealed class ExpressionAlias : SqlPart, ISortable
{
    private readonly SqlExpression _expr;

    // When the alias names a CTE / derived-table handle column (As(DbColumn)),
    // it must be emitted bare — exactly as that column is later referenced
    // (DbColumn renders the name unquoted). Quoting only the definition would,
    // on case-folding engines like Oracle, leave the definition lowercase while
    // the reference folds to uppercase, so the column cannot be resolved (#165).
    // A string alias (As("...")) stays alias-quoted: it is arbitrary text the
    // caller chose and is only ever referenced through this same handle.
    private readonly bool _quoteAlias;

    internal ExpressionAlias(SqlExpression expr, string name, bool quoteAlias = true)
    {
        _expr = expr;
        Name = name;
        _quoteAlias = quoteAlias;
    }

    internal string Name { get; }

    /// <summary>
    /// Gets the ascending <c>ORDER BY</c> sort key for this alias
    /// (<c>"alias" ASC</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new(this, SortDirection.Asc);

    /// <summary>
    /// Gets the descending <c>ORDER BY</c> sort key for this alias
    /// (<c>"alias" DESC</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new(this, SortDirection.Desc);

    /// <summary>
    /// Gets the <c>ORDER BY</c> sort key that puts <see langword="null"/>
    /// values first (<c>"alias" NULLS FIRST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new(this, NullOrdering.NullsFirst);

    /// <summary>
    /// Gets the <c>ORDER BY</c> sort key that puts <see langword="null"/>
    /// values last (<c>"alias" NULLS LAST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new(this, NullOrdering.NullsLast);

    internal override void Format(SqlBuildingBuffer buffer) => AppendAlias(buffer);

    internal void FormatAsSelect(SqlBuildingBuffer buffer)
    {
        buffer.AppendSpace(_expr);

        // Oracle's WITH RECURSIVE needs the explicit AS here (#263); every other
        // position accepts the bare form, so this stays off outside that scope.
        if (buffer.RequireExplicitColumnAlias)
        {
            buffer.Append(Keywords.As).AppendSpace();
        }

        AppendAlias(buffer);
    }

    private void AppendAlias(SqlBuildingBuffer buffer)
    {
        if (_quoteAlias)
        {
            buffer.EncloseInAliasQuotes(Name);
        }
        else
        {
            buffer.Append(Name);
        }
    }
}
