namespace InlineSqlSharp;

public sealed class CountFunction(bool distinct, IExpr expr) : AggregateFunction
{
	private readonly bool _distinct = distinct;
	private readonly IExpr _expr = expr;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.COUNT)
		.OpenParenthesis()
		.AppendSpaceIf(_distinct, Keywords.DISTINCT)
		.Append(_expr)
		.CloseParenthesis();
}
