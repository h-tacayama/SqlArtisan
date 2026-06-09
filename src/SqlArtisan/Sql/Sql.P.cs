using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static PartitionByClause PartitionBy(
        params object[] expressions) => new(Resolve(expressions));

    public static AnalyticPercentRankFunction PercentRank() => new();

    /// <summary>
    /// A <c>n PRECEDING</c> window-frame bound (offset rows/range before the
    /// current row).
    /// </summary>
    public static FrameBound Preceding(int offset) => FrameBound.Preceding(offset);
}
