namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static CharacterLiteral L(string value) => new(value);

	public static CountFunction COUNT(IExpr expr) => new(expr);
}
