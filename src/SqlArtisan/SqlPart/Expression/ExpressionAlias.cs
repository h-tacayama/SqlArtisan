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

    // A CTE / derived-table handle column (As(DbColumn)) must be emitted bare,
    // exactly as DbColumn later references it: quoting only the definition leaves
    // it lowercase on a case-folding engine like Oracle, where the reference folds
    // to uppercase and no longer resolves (#165).
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
    /// <remarks>Not available on MySQL or SQL Server; SQLite (3.30+).</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new(this, NullOrdering.NullsFirst);

    /// <summary>
    /// Gets the <c>ORDER BY</c> sort key that puts <see langword="null"/>
    /// values last (<c>"alias" NULLS LAST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite (3.30+).</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new(this, NullOrdering.NullsLast);

    internal override void Format(SqlBuildingBuffer buffer) => AppendAlias(buffer);

    internal void FormatAsSelect(SqlBuildingBuffer buffer)
    {
        buffer.AppendSpace(_expr);
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
