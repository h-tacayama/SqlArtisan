using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static PartitionByClause PartitionBy(
        params object[] expressions) => new(Resolve(expressions));

    public static AnalyticPercentRankFunction PercentRank() => new();
}
