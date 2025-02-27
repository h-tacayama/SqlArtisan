namespace InlineSqlSharp;

internal sealed class MinFunctionCore<TExpr>(TExpr expr)
	where TExpr : IDataExpr
{
	private readonly TExpr _expr = expr;

	internal void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.MIN)
		.OpenParenthesis()
		.Append(_expr)
		.CloseParenthesis();
}
