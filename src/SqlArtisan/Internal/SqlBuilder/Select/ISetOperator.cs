using System.Diagnostics;

namespace SqlArtisan.Internal;

/// <summary>
/// The set operators that combine the current query with a following <c>SELECT</c>.
/// </summary>
public interface ISetOperator
{
    /// <summary>
    /// Combines with the next query using <c>EXCEPT</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Except { get; }

    /// <summary>
    /// Combines with the next query using <c>EXCEPT ALL</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator ExceptAll { get; }

    /// <summary>
    /// Combines with the next query using <c>INTERSECT</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Intersect { get; }

    /// <summary>
    /// Combines with the next query using <c>INTERSECT ALL</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator IntersectAll { get; }

    /// <summary>
    /// Combines with the next query using <c>MINUS</c> (Oracle).
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Minus { get; }

    /// <summary>
    /// Combines with the next query using <c>MINUS ALL</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator MinusAll { get; }

    /// <summary>
    /// Combines with the next query using <c>UNION</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator Union { get; }

    /// <summary>
    /// Combines with the next query using <c>UNION ALL</c>.
    /// </summary>
    /// <value>The builder positioned to supply the next <c>SELECT</c>.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ISelectBuilderSetOperator UnionAll { get; }
}
