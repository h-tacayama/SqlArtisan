namespace InlineSqlSharp;

public sealed class AnalyticDenseRankFunction : AbstractAnalyticFunction
{
    internal AnalyticDenseRankFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DenseRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
