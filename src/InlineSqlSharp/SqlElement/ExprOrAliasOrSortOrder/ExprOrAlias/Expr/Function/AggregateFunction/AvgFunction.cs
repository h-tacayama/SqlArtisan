namespace InlineSqlSharp;

public sealed class AvgFunction(AllOrDistinct allOrDistinct, IExpr expr)
	: AggregateFunction
{
	private readonly AllOrDistinct _allOrDistinct = allOrDistinct;
	private readonly IExpr _expr = expr;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.AVG)
		.OpenParenthesis()
		.AppendSpaceIf(_allOrDistinct.IsDistinct, _allOrDistinct)
		.Append(_expr)
		.CloseParenthesis();
}
