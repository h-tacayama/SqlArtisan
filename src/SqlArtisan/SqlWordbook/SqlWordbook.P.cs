using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static PartitionByClause PartitionBy(
        params object[] expressions) => new(Resolve(expressions));

    public static AnalyticPercentRankFunction PercentRank() => new();
}
