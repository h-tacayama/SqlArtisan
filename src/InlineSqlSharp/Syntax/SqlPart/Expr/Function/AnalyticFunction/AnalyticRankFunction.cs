namespace InlineSqlSharp;

public sealed class AnalyticRankFunction() : AbstractAnalyticFunction
{
    public WindowFunction OVER(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction OVER(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RANK)
        .OpenParenthesis()
        .CloseParenthesis();
}
