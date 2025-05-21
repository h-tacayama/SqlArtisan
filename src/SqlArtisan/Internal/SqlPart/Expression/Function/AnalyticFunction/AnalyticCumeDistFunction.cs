namespace SqlArtisan.Internal;

public sealed class AnalyticCumeDistFunction : AnalyticFunction
{
    internal AnalyticCumeDistFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.CumeDist)
        .OpenParenthesis()
        .CloseParenthesis();
}
