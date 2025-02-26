namespace InlineSqlSharp;

public sealed class SearchedCaseWhenClause<TReturnExpr>(
	SearchedCaseWhenCondition whenCondition,
	CaseThenExpr<TReturnExpr> thenExpr) : ISqlElement
	where TReturnExpr : IDataExpr
{
	private readonly SearchedCaseWhenCondition _whenCondition = whenCondition;
	private readonly CaseThenExpr<TReturnExpr> _thenExpr = thenExpr;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.WHEN)
		.OpenParenthesis()
		.Append(_whenCondition)
		.CloseParenthesis()
		.EncloseInSpaces(Keywords.THEN)
		.Append(_thenExpr);
}
