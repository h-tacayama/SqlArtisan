using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class ExprAlias(IExpr expr, AliasName alias) :
	IExprOrAlias,
	ISortable
{
	private readonly IExpr _expr = expr;
	private readonly AliasName _alias = alias;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder ASC => new(this, SortDirection.Asc);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder DESC => new(this, SortDirection.Desc);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_FIRST => new(this, NullOrdering.NullsFirst);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_LAST => new(this, NullOrdering.NullsLast);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Append(_alias.Name);

	public void FormatAsSelect(ref SqlBuildingBuffer buffer) => buffer
		.AppendSpace(_expr)
		.Append(_alias.Name);
}
