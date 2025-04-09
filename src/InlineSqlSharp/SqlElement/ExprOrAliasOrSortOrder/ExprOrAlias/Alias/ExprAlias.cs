using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class ExprAlias(IExpr expr, string alias) :
    IExprOrAlias,
    ISortable
{
    private readonly IExpr _expr = expr;
    private readonly string _alias = alias;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder ASC => new(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder DESC => new(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_FIRST => new(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_LAST => new(this, NullOrdering.NullsLast);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.EncloseInDoubleQuotes(_alias);

    public void FormatAsSelect(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_expr)
        .AppendSpace(Keywords.AS)
        .EncloseInDoubleQuotes(_alias);
}
