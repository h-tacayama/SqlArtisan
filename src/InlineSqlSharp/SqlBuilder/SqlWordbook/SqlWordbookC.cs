namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterSearchedCaseExpr CASE(
		SearchedCaseWhenClause<CharacterExpr> whenClause,
		CaseElseExpr<CharacterExpr> elseExpr) =>
		new([whenClause], elseExpr);

	public static CharacterSearchedCaseExpr CASE(
		SearchedCaseWhenClause<CharacterExpr> whenClause1,
		SearchedCaseWhenClause<CharacterExpr> whenClause2,
		CaseElseExpr<CharacterExpr> elseExpr) =>
		new([whenClause1, whenClause2], elseExpr);

	public static CharacterSearchedCaseExpr CASE(
		SearchedCaseWhenClause<CharacterExpr> whenClause1,
		SearchedCaseWhenClause<CharacterExpr> whenClause2,
		SearchedCaseWhenClause<CharacterExpr> whenClause3,
		CaseElseExpr<CharacterExpr> elseExpr) =>
		new([whenClause1, whenClause2, whenClause3], elseExpr);

	public static DateTimeSearchedCaseExpr CASE(
		SearchedCaseWhenClause<DateTimeExpr> whenClause,
		CaseElseExpr<DateTimeExpr> elseExpr) =>
		new([whenClause], elseExpr);

	public static DateTimeSearchedCaseExpr CASE(
		SearchedCaseWhenClause<DateTimeExpr> whenClause1,
		SearchedCaseWhenClause<DateTimeExpr> whenClause2,
		CaseElseExpr<DateTimeExpr> elseExpr) =>
		new([whenClause1, whenClause2], elseExpr);

	public static DateTimeSearchedCaseExpr CASE(
		SearchedCaseWhenClause<DateTimeExpr> whenClause1,
		SearchedCaseWhenClause<DateTimeExpr> whenClause2,
		SearchedCaseWhenClause<DateTimeExpr> whenClause3,
		CaseElseExpr<DateTimeExpr> elseExpr) =>
		new([whenClause1, whenClause2, whenClause3], elseExpr);

	public static NumericSearchedCaseExpr CASE(
		SearchedCaseWhenClause<NumericExpr> whenClause,
		CaseElseExpr<NumericExpr> elseExpr) =>
		new([whenClause], elseExpr);

	public static NumericSearchedCaseExpr CASE(
		SearchedCaseWhenClause<NumericExpr> whenClause1,
		SearchedCaseWhenClause<NumericExpr> whenClause2,
		CaseElseExpr<NumericExpr> elseExpr) =>
		new([whenClause1, whenClause2], elseExpr);

	public static NumericSearchedCaseExpr CASE(
		SearchedCaseWhenClause<NumericExpr> whenClause1,
		SearchedCaseWhenClause<NumericExpr> whenClause2,
		SearchedCaseWhenClause<NumericExpr> whenClause3,
		CaseElseExpr<NumericExpr> elseExpr) =>
		new([whenClause1, whenClause2, whenClause3], elseExpr);

	public static CharacterSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause,
		CaseElseExpr<CharacterExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause], elseExpr);

	public static CharacterSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause2,
		CaseElseExpr<CharacterExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2], elseExpr);

	public static CharacterSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause2,
		SimpleCaseWhenClause<TComparisonExpr, CharacterExpr> whenClause3,
		CaseElseExpr<CharacterExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2, whenClause3], elseExpr);

	public static DateTimeSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause,
		CaseElseExpr<DateTimeExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause], elseExpr);

	public static DateTimeSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause2,
		CaseElseExpr<DateTimeExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2], elseExpr);

	public static DateTimeSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause2,
		SimpleCaseWhenClause<TComparisonExpr, DateTimeExpr> whenClause3,
		CaseElseExpr<DateTimeExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2, whenClause3], elseExpr);

	public static NumericSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause,
		CaseElseExpr<NumericExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause], elseExpr);

	public static NumericSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause2,
		CaseElseExpr<NumericExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2], elseExpr);

	public static NumericSimpleCaseExpr<TComparisonExpr> CASE<TComparisonExpr>(
		TComparisonExpr expr,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause1,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause2,
		SimpleCaseWhenClause<TComparisonExpr, NumericExpr> whenClause3,
		CaseElseExpr<NumericExpr> elseExpr)
		where TComparisonExpr : IDataExpr =>
			new(expr, [whenClause1, whenClause2, whenClause3], elseExpr);

	public static ConcatFunction CONCAT(
		CharacterExpr primary,
		CharacterExpr secondary,
		params CharacterExpr[] others) => new(primary, secondary, others);

	public static CountFunction COUNT(IExpr expr) => new(AllOrDistinct.All, expr);

	public static CountFunction COUNT(AllOrDistinct allOrDistinct, IExpr expr) =>
		new(allOrDistinct, expr);
}
