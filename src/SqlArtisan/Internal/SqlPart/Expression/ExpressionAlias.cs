using System.Diagnostics;

namespace SqlArtisan.Internal;

public sealed class ExpressionAlias : SqlPart, ISortable
{
    private readonly SqlExpression _expr;
    private readonly string _name;

    internal ExpressionAlias(SqlExpression expr, string name)
    {
        _expr = expr;
        _name = name;
    }

    internal string Name => _name;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new(this, NullOrdering.NullsLast);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInAliasQuotes(_name);

    internal void FormatAsSelect(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_expr)
        .EncloseInAliasQuotes(_name);
}
