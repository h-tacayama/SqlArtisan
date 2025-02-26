namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CaseThenExpr<CharacterExpr> THEN(CharacterExpr thenExpr) =>
		new(thenExpr);

	public static CaseThenExpr<DateTimeExpr> THEN(DateTimeExpr thenExpr) =>
		new(thenExpr);

	public static CaseThenExpr<NumericExpr> THEN(NumericExpr thenExpr) =>
		new(thenExpr);

	public static ToDateFunction TO_DATE(string text, string format) =>
		new(text, format);
}
