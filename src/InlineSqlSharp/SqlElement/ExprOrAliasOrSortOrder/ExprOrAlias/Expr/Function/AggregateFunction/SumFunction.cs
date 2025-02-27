namespace InlineSqlSharp;

public sealed class SumFunction(AllOrDistinct allOrDistinct, IExpr expr)
	: AggregateFunction
{
	private readonly AllOrDistinctFunctionCore _core =
		new(Keywords.SUM, allOrDistinct, expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
