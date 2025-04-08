namespace InlineSqlSharp;

public sealed class SimpleCaseWhenClause<TWhenExpr, TReturnExpr>(
	SimpleCaseWhenExpr<TWhenExpr> whenExpr,
	CaseThenExpr<TReturnExpr> thenExpr) : ISqlElement
	where TWhenExpr : IExpr
	where TReturnExpr : IExpr
{
	private readonly SimpleCaseWhenExpr<TWhenExpr> _whenExpr = whenExpr;
	private readonly CaseThenExpr<TReturnExpr> _thenExpr = thenExpr;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.WHEN)
		.AppendSpace(_whenExpr)
		.AppendSpace(Keywords.THEN)
		.Append(_thenExpr);
}
