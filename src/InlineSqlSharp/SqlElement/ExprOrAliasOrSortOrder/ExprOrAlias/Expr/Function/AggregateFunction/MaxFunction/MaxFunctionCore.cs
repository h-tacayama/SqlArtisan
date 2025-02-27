namespace InlineSqlSharp;

internal sealed class MaxFunctionCore<TExpr>(TExpr expr)
	where TExpr : IDataExpr
{
	private readonly TExpr _expr = expr;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.MAX)
		.OpenParenthesis()
		.Append(_expr)
		.CloseParenthesis();
}
