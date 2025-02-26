namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static SearchedCaseWhenCondition WHEN(ICondition whenCondition) =>
		new(whenCondition);

	public static SimpleCaseWhenExpr<CharacterExpr> WHEN(CharacterExpr whenExpr) =>
		new(whenExpr);

	public static SimpleCaseWhenExpr<DateTimeExpr> WHEN(DateTimeExpr whenExpr) =>
		new(whenExpr);

	public static SimpleCaseWhenExpr<NumericExpr> WHEN(NumericExpr whenExpr) =>
		new(whenExpr);
}
