using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>UNBOUNDED FOLLOWING</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound UnboundedFollowing => FrameBound.UnboundedFollowing();

    /// <summary>
    /// The <c>UNBOUNDED PRECEDING</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound UnboundedPreceding => FrameBound.UnboundedPreceding();

    public static IUpdateBuilderUpdate Update(DbTableBase table) =>
        new UpdateBuilder(new UpdateClause(table));

    public static UpperFunction Upper(object source) =>
        new(Resolve(source));
}
