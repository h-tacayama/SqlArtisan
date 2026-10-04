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

    // As(DbColumn) copies the column's quoting, so the definition renders as DbColumn
    // references it (#165).
    private readonly bool _quoteAlias;

    internal ExpressionAlias(SqlExpression expr, string name, bool quoteAlias = true)
    {
        StringGuard.ThrowIfNullOrEmpty(name, "An expression alias requires a name.");

        _expr = expr;
        Name = name;
        _quoteAlias = quoteAlias;
    }

    internal string Name { get; }

    internal bool QuoteAlias => _quoteAlias;

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

    internal const string StringLeftOfAliasMessage =
        "An alias names a whole SELECT-list item; alias the sum instead: (\"x\" + col).As(...).";

    internal const string StringRightOfAliasMessage =
        "An alias names a whole SELECT-list item; alias the sum instead: (col + \"x\").As(...).";

    /// <summary>
    /// Not supported: <c>"Dr. " + col.As("n")</c> adds a string to an alias, because
    /// <c>.As(...)</c> binds before <c>+</c>. Alias the whole sum instead:
    /// <c>("Dr. " + col).As("n")</c>.
    /// </summary>
    /// <param name="leftSide">The string.</param>
    /// <param name="rightSide">The alias.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(StringLeftOfAliasMessage, error: true)]
    public static AdditionOperator operator +(string leftSide, ExpressionAlias rightSide) =>
        throw new InvalidOperationException(StringLeftOfAliasMessage);

    /// <summary>
    /// Not supported: <c>col.As("n") + " Jr"</c> adds a string to an alias, because
    /// <c>.As(...)</c> binds before <c>+</c>. Alias the whole sum instead:
    /// <c>(col + " Jr").As("n")</c>.
    /// </summary>
    /// <param name="leftSide">The alias.</param>
    /// <param name="rightSide">The string.</param>
    /// <returns>Never returns; the call does not compile.</returns>
    [Obsolete(StringRightOfAliasMessage, error: true)]
    public static AdditionOperator operator +(ExpressionAlias leftSide, string rightSide) =>
        throw new InvalidOperationException(StringRightOfAliasMessage);

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
