namespace InlineSqlSharp;

public sealed class ToNumberFunction(
	CharacterExpr expr,
	CharacterExpr? format = null) : CharacterExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.TO_NUMBER, expr, format);


	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
