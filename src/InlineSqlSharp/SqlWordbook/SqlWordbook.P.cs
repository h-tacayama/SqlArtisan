using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static PartitionByClause PARTITION_BY(
        params object[] expressions) => new(Resolve(expressions));

    public static AnalyticPercentRankFunction PERCENT_RANK() => new();
}
