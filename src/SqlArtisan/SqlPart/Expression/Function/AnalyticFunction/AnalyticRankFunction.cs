namespace SqlArtisan;

public sealed class AnalyticRankFunction : AnalyticFunction
{
    internal AnalyticRankFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Rank)
        .OpenParenthesis()
        .CloseParenthesis();
}
