namespace InlineSqlSharp;

public sealed class CharacterMaxFunction(CharacterExpr expr) : CharacterExpr
{
	private readonly MaxFunctionCore<CharacterExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
