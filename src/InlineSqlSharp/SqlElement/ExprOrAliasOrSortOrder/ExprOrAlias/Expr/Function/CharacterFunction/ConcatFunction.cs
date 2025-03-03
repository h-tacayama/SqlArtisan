namespace InlineSqlSharp;

public sealed class ConcatFunction(
	CharacterExpr primary,
	CharacterExpr secondary,
	CharacterExpr[] others) : CharacterExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.CONCAT, [primary, secondary, .. others]);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
