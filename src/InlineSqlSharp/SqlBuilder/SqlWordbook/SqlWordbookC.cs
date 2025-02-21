namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CountFunction COUNT(IExpr expr) => new(false, expr);

	public static CountFunction COUNT_DISTINCT(IExpr expr) => new(true, expr);
}
