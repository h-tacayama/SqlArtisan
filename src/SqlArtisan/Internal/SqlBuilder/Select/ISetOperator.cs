using System.Diagnostics;

namespace SqlArtisan.Internal;

/// <summary>The set operators that combine the current query with a following <c>SELECT</c>. Each <c>*All</c> variant keeps duplicate rows; the plain form removes them.</summary>
public interface ISetOperator
{
    /// <summary>Combines with the next query using <c>EXCEPT</c> (rows in the first query but not the second, duplicates removed).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Except { get; }

    /// <summary>Combines with the next query using <c>EXCEPT ALL</c> (as <see cref="Except"/>, but duplicate rows are kept).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator ExceptAll { get; }

    /// <summary>Combines with the next query using <c>INTERSECT</c> (rows present in both queries, duplicates removed).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Intersect { get; }

    /// <summary>Combines with the next query using <c>INTERSECT ALL</c> (as <see cref="Intersect"/>, but duplicate rows are kept).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator IntersectAll { get; }

    /// <summary>Combines with the next query using <c>MINUS</c>, Oracle's spelling of <see cref="Except"/> (rows in the first query but not the second).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Minus { get; }

    /// <summary>Combines with the next query using <c>MINUS ALL</c> (as <see cref="Minus"/>, but duplicate rows are kept).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator MinusAll { get; }

    /// <summary>Combines with the next query using <c>UNION</c> (all rows from both queries, duplicates removed).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Union { get; }

    /// <summary>Combines with the next query using <c>UNION ALL</c> (as <see cref="Union"/>, but duplicate rows are kept).</summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator UnionAll { get; }
}
