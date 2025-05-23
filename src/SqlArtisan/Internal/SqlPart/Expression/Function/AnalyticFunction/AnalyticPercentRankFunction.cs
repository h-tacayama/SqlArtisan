namespace SqlArtisan.Internal;

public sealed class AnalyticPercentRankFunction : AnalyticFunction
{
    internal AnalyticPercentRankFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.PercentRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
