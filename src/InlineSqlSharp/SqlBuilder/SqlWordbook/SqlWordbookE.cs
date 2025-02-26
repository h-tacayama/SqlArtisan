namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CaseElseExpr<CharacterExpr> ELSE(CharacterExpr thenExpr) =>
		new(thenExpr);

	public static CaseElseExpr<DateTimeExpr> ELSE(DateTimeExpr thenExpr) =>
		new(thenExpr);

	public static CaseElseExpr<NumericExpr> ELSE(NumericExpr thenExpr) =>
		new(thenExpr);

	public static ExistsCondition EXISTS(ISubquery subquery) =>
		new(subquery);
}