namespace InlineSqlSharp;

public sealed class CharacterMinFunction(CharacterExpr expr) : CharacterExpr
{
	readonly UnaryFunctionCore _core = new(Keywords.MIN, expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
