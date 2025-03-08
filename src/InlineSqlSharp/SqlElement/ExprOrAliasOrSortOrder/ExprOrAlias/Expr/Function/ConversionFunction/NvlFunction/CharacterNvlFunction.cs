namespace InlineSqlSharp;

public sealed class CharacterNvlFunction(
	CharacterExpr expr1,
	CharacterExpr expr2) : CharacterExpr
{
	private readonly BinaryFunctionCore _core = new(Keywords.NVL, expr1, expr2);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
