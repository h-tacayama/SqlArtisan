namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CaseThenExpr<CharacterExpr> THEN(CharacterExpr thenExpr) =>
		new(thenExpr);

	public static CaseThenExpr<DateTimeExpr> THEN(DateTimeExpr thenExpr) =>
		new(thenExpr);

	public static CaseThenExpr<NumericExpr> THEN(NumericExpr thenExpr) =>
		new(thenExpr);

	public static ToDateFunction TO_DATE(
		CharacterExpr text,
		CharacterExpr format) => new(text, format);

	public static TrimFunction TRIM(CharacterExpr source) => new(source);

	public static TrimFunction TRIM(
		CharacterExpr source,
		CharacterExpr trimChar) => new(source, trimChar);

	public static ToCharFunction TO_CHAR(
		DateTimeExpr expr) => ToCharFunction.Of(expr);

	public static ToCharFunction TO_CHAR(
		DateTimeExpr expr,
		CharacterExpr datetimeFormat) => ToCharFunction.Of(expr, datetimeFormat);

	public static ToCharFunction TO_CHAR(
		NumericExpr expr) => ToCharFunction.Of(expr);

	public static ToCharFunction TO_CHAR(
		NumericExpr expr,
		CharacterExpr numericFormat) => ToCharFunction.Of(expr, numericFormat);

	public static ToNumberFunction TO_NUMBER(CharacterExpr expr) => new(expr);

	public static ToNumberFunction TO_NUMBER(
		CharacterExpr expr,
		CharacterExpr numericFormat) => new(expr, numericFormat);

	public static NumericTruncFunction TRUNC(
		NumericExpr expr) => new(expr);

	public static NumericTruncFunction TRUNC(
		NumericExpr expr,
		NumericExpr decimalPlaces) => new(expr, decimalPlaces);
}
