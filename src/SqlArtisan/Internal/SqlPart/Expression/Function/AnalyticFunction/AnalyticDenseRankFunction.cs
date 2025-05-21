namespace SqlArtisan.Internal;

public sealed class AnalyticDenseRankFunction : AnalyticFunction
{
    internal AnalyticDenseRankFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DenseRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
