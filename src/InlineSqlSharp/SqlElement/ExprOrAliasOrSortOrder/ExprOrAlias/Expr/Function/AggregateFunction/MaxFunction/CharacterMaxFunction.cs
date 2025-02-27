namespace InlineSqlSharp;

public sealed class CharacterMaxFunction(CharacterExpr expr) : CharacterExpr
{
	private readonly UnaryFunctionCore _core = new(Keywords.MAX, expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
