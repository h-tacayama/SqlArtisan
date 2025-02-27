namespace InlineSqlSharp;

public sealed class CharacterMinFunction(CharacterExpr expr) : CharacterExpr
{
	private readonly MinFunctionCore<CharacterExpr> _core = new(expr);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
