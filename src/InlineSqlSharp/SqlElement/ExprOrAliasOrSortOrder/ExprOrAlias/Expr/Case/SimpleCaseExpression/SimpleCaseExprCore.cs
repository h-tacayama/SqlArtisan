namespace InlineSqlSharp;

internal sealed class SimpleCaseExprCore<TComparisonExpr, TReturnExpr>(
	TComparisonExpr expr,
	SimpleCaseWhenClause<TComparisonExpr, TReturnExpr>[] whenClauses,
	CaseElseExpr<TReturnExpr> elseClause)
	where TComparisonExpr : IDataExpr
	where TReturnExpr : IDataExpr
{
	private readonly TComparisonExpr _expr = expr;
	private readonly SimpleCaseWhenClause<TComparisonExpr, TReturnExpr>[] _whenClauses = whenClauses;
	private readonly CaseElseExpr<TReturnExpr> _elseClause = elseClause;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.CASE)
		.Append(_expr)
		.AppendLine()
		.AppendLineSeparated(_whenClauses)
		.AppendLine()
		.AppendSpace(Keywords.ELSE)
		.AppendLine(_elseClause)
		.Append(Keywords.END);
}
