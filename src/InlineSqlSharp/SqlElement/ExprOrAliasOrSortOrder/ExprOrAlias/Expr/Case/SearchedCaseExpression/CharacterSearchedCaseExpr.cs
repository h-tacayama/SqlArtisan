namespace InlineSqlSharp;

public sealed class CharacterSearchedCaseExpr(
	SearchedCaseWhenClause<CharacterExpr>[] whenClauses,
	CaseElseExpr<CharacterExpr> elseClause)
	: CharacterExpr,
	ISearchedCaseExpression
{
	private readonly SearchedCaseExprCore<CharacterExpr> _core =
		new(whenClauses, elseClause);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
