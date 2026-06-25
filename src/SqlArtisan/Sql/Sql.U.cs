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
    /// Begins an <c>UPDATE table</c> statement. Continue with <c>.Set(...)</c> and
    /// <c>.Where(...)</c>.
    /// </summary>
    /// <param name="table">The table to update.</param>
    /// <returns>An update builder positioned for <c>.Set(...)</c>.</returns>
    public static IUpdateBuilderUpdate Update(DbTableBase table) =>
        new UpdateBuilder(new UpdateClause(table));

    /// <summary>
    /// The <c>UPPER(source)</c> function (uppercases <paramref name="source"/>).
    /// </summary>
    /// <param name="source">The string to uppercase.</param>
    /// <returns>An <c>UPPER</c> function expression.</returns>
    public static UpperFunction Upper(object source) =>
        new(Resolve(source));
}
