namespace InlineSqlSharp;

internal sealed class SearchedCaseExprCore<TReturnExpr>(
	SearchedCaseWhenClause<TReturnExpr>[] whenClauses,
	CaseElseExpr<TReturnExpr> elseClause)
	where TReturnExpr : IDataExpr
{
	private readonly SearchedCaseWhenClause<TReturnExpr>[] _whenClauses = whenClauses;
	private readonly CaseElseExpr<TReturnExpr> _elseClause = elseClause;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.CASE)
		.AppendLineSeparated(_whenClauses)
		.AppendLine()
		.AppendSpace(Keywords.ELSE)
		.AppendLine(_elseClause)
		.Append(Keywords.END);
}