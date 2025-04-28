using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class ExprAlias : AbstractSqlPart, ISortable
{
    private readonly AbstractExpr _expr;
    private readonly string _alias;

    internal ExprAlias(AbstractExpr expr, string alias)
    {
        _expr = expr;
        _alias = alias;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new SortOrder(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new SortOrder(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new SortOrder(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new SortOrder(this, NullOrdering.NullsLast);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.EncloseInDoubleQuotes(_alias);

    internal void FormatAsSelect(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_expr)
        .AppendSpace(Keywords.As)
        .EncloseInDoubleQuotes(_alias);
}
