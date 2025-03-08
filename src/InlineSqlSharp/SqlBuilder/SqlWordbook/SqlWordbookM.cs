namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterMaxFunction MAX(CharacterExpr expr) => new(expr);

	public static DateTimeMaxFunction MAX(DateTimeExpr expr) => new(expr);

	public static NumericMaxFunction MAX(NumericExpr expr) => new(expr);

	public static CharacterMinFunction MIN(CharacterExpr expr) => new(expr);

	public static DateTimeMinFunction MIN(DateTimeExpr expr) => new(expr);

	public static NumericMinFunction MIN(NumericExpr expr) => new(expr);

	public static ModFunction MOD(
		NumericExpr dividend,
		NumericExpr divisor) => new(dividend, divisor);

	public static MonthsBetweenFunction MONTHS_BETWEEN(
		DateTimeExpr date1,
		DateTimeExpr date2) => new(date1, date2);
}
