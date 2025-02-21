namespace InlineSqlSharp;

public sealed class CountFunction(AllOrDistinct allOrDistinct, IExpr expr) : AggregateFunction
{
	private readonly AllOrDistinct _allOrDistinct = allOrDistinct;
	private readonly IExpr _expr = expr;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.COUNT)
		.OpenParenthesis()
		.AppendSpaceIf(_allOrDistinct.IsDistinct, _allOrDistinct)
		.Append(_expr)
		.CloseParenthesis();
}
