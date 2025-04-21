namespace InlineSqlSharp;

public sealed class AnalyticRowNumberFunction : AbstractAnalyticFunction
{
    internal AnalyticRowNumberFunction() { }

    public WindowFunction OVER(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    public WindowFunction OVER(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ROW_NUMBER)
        .OpenParenthesis()
        .CloseParenthesis();
}
