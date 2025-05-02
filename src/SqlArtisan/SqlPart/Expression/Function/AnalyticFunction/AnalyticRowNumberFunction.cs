namespace SqlArtisan;

public sealed class AnalyticRowNumberFunction : AnalyticFunction
{
    internal AnalyticRowNumberFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RowNumber)
        .OpenParenthesis()
        .CloseParenthesis();
}
