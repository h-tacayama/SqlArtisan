namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterSearchedCaseExpr CASE(
		SearchedCaseWhenClause<CharacterExpr>[] whenClauses,
		CaseElseExpr<CharacterExpr> elseExpr) =>
		new(whenClauses, elseExpr);

	public static DateTimeSearchedCaseExpr CASE(
		SearchedCaseWhenClause<DateTimeExpr>[] whenClauses,
		CaseElseExpr<DateTimeExpr> elseExpr) =>
		new(whenClauses, elseExpr);

	public static NumericSearchedCaseExpr CASE(
		SearchedCaseWhenClause<NumericExpr>[] whenClauses,
		CaseElseExpr<NumericExpr> elseExpr) =>
		new(whenClauses, elseExpr);

	public static CharacterSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr>[] whenClauses,
		CaseElseExpr<CharacterExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, whenClauses, elseExpr);

	public static DateTimeSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr>[] whenClauses,
		CaseElseExpr<DateTimeExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, whenClauses, elseExpr);

	public static NumericSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr>[] whenClauses,
		CaseElseExpr<NumericExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, whenClauses, elseExpr);

	public static ConcatFunction CONCAT(
		CharacterExpr primary,
		CharacterExpr secondary,
		params CharacterExpr[] others) => new(primary, secondary, others);

	public static CountFunction COUNT(IExpr expr) => new(AllOrDistinct.All, expr);

	public static CountFunction COUNT(AllOrDistinct allOrDistinct, IExpr expr) =>
		new(allOrDistinct, expr);

	public static AnalyticCumeDistFunction CUME_DIST() => new();
}
