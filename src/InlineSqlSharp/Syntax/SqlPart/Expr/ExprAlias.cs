using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class ExprAlias(AbstractExpr expr, string alias) :
    AbstractSqlPart,
    ISortable
{
    private readonly AbstractExpr _expr = expr;
    private readonly string _alias = alias;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder ASC => new SortOrder(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder DESC => new SortOrder(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_FIRST => new SortOrder(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_LAST => new SortOrder(this, NullOrdering.NullsLast);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.EncloseInDoubleQuotes(_alias);

    internal void FormatAsSelect(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_expr)
        .AppendSpace(Keywords.AS)
        .EncloseInDoubleQuotes(_alias);
}
