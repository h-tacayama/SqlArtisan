namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CountFunction COUNT(IExpr expr) => new(AllOrDistinct.All, expr);

	public static CountFunction COUNT(AllOrDistinct allOrDistinct, IExpr expr) =>
		new(allOrDistinct, expr);
}
