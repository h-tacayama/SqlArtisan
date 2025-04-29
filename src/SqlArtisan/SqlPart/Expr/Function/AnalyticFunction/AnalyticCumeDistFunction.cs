namespace SqlArtisan;

public sealed class AnalyticCumeDistFunction : AbstractAnalyticFunction
{
    internal AnalyticCumeDistFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.CumeDist)
        .OpenParenthesis()
        .CloseParenthesis();
}
