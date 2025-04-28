using System.Diagnostics;

namespace InlineSqlSharp;

public interface ISetOperator
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Except { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator ExceptAll { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Intersect { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator IntersectAll { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Minus { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator MinusAll { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Union { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator UnionAll { get; }
}
