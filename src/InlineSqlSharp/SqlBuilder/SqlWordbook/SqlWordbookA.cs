namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static AbsFunction ABS(NumericExpr expr) => new(expr);

	public static DynamicCondition AddConditionIf(
		bool addIf,
		ICondition condition) => new(addIf, condition);

	public static AddMonthsFunction ADD_MONTHS(
		DateTimeExpr dateTime,
		NumericExpr months) => new(dateTime, months);

	public static AndCondition AND(params ICondition[] conditions) =>
		new(conditions);

	public static AvgFunction AVG(IExpr expr) => new(AllOrDistinct.All, expr);

	public static AvgFunction AVG(AllOrDistinct allOrDistinct, IExpr expr) =>
		new(allOrDistinct, expr);
}
