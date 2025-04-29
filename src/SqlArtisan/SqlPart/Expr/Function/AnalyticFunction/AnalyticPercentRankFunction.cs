namespace InlineSqlSharp;

public sealed class AnalyticPercentRankFunction : AbstractAnalyticFunction
{
    internal AnalyticPercentRankFunction() { }

    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.PercentRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
