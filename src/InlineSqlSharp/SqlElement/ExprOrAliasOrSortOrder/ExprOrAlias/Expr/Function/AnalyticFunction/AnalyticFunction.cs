namespace InlineSqlSharp;

public abstract class AnalyticFunction() : NumericExpr
{
	public WindowFunction OVER() =>
		new(this, OverClause.Of());

	public WindowFunction OVER(PartitionByClause partitionByClause) =>
		new(this, OverClause.Of(partitionByClause));

	public WindowFunction OVER(PartitionAndOrderByClause orderByClause) =>
		new(this, OverClause.Of(orderByClause));
}
