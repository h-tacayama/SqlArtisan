using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>UNBOUNDED PRECEDING</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound UnboundedPreceding => FrameBound.UnboundedPreceding();

    /// <summary>
    /// The <c>UNBOUNDED FOLLOWING</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound UnboundedFollowing => FrameBound.UnboundedFollowing();

    /// <summary>
    /// The <c>UNNEST(arrays)</c> table function: expands one or more arrays into
    /// rows (PostgreSQL). Name it as a <c>FROM</c> source with
    /// <see cref="UnnestFunction.AsTable(string)"/>; in a <c>SELECT</c> list it
    /// is a set-returning call.
    /// </summary>
    /// <param name="arrays">The array expressions to expand — e.g.
    /// <see cref="BindArray{T}(T[])"/> values or <c>ARRAY[...]</c> constructors;
    /// at least one.</param>
    /// <returns>An <see cref="UnnestFunction"/> emitting <c>UNNEST(arrays)</c>.</returns>
    public static UnnestFunction Unnest(params object[] arrays)
    {
        CollectionGuard.ThrowIfEmpty(arrays, "UNNEST(...) requires at least one array.");
        return new(Resolve(arrays));
    }

    /// <summary>
    /// Begins an <c>UPDATE table</c> statement. Continue with <c>.Set(...)</c> and
    /// <c>.Where(...)</c>.
    /// </summary>
    /// <param name="table">The table to update.</param>
    /// <returns>An update builder positioned for <c>.Set(...)</c>.</returns>
    public static IUpdateBuilderUpdate Update(DbTableBase table)
    {
        DmlJoinState state = new();
        UpdateClause updateClause = new(table, state);
        return new UpdateBuilder(table, state, updateClause);
    }

    /// <summary>
    /// The <c>UPPER(source)</c> function (uppercases <paramref name="source"/>).
    /// </summary>
    /// <param name="source">The string to uppercase.</param>
    /// <returns>An <c>UPPER</c> function expression.</returns>
    public static UpperFunction Upper(object source) =>
        new(Resolve(source));
}
