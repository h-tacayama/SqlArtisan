namespace InlineSqlSharp;

public sealed class DateTimeSearchedCaseExpr(
	SearchedCaseWhenClause<DateTimeExpr>[] whenClauses,
	CaseElseExpr<DateTimeExpr> elseClause) :
	DateTimeExpr,
	ISearchedCaseExpression
{
	private readonly SearchedCaseExprCore<DateTimeExpr> _core =
		new(whenClauses, elseClause);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
