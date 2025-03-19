namespace InlineSqlSharp;

internal sealed class SearchedCaseExprCore<TReturnExpr>(
	SearchedCaseWhenClause<TReturnExpr>[] whenClauses,
	CaseElseExpr<TReturnExpr> elseClause)
	where TReturnExpr : IDataExpr
{
	private readonly SearchedCaseWhenClause<TReturnExpr>[] _whenClauses = whenClauses;
	private readonly CaseElseExpr<TReturnExpr> _elseClause = elseClause;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.CASE)
		.AppendSpaceSeparated(_whenClauses)
		.AppendSpace()
		.AppendSpace(Keywords.ELSE)
		.AppendSpace(_elseClause)
		.Append(Keywords.END);
}