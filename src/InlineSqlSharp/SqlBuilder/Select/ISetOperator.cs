using System.Diagnostics;

namespace InlineSqlSharp;

public interface ISetOperator
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator EXCEPT { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator EXCEPT_ALL { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator INTERSECT { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator INTERSECT_ALL { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator MINUS { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator MINUS_ALL { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator UNION { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	ISelectBuilderSetOperator UNION_ALL { get; }
}
