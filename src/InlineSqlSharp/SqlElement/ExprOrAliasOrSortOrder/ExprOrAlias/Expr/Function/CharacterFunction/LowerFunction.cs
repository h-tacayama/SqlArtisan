namespace InlineSqlSharp;

public sealed class LowerFunction(CharacterExpr source) : CharacterExpr
{
	private readonly UnaryFunctionCore _core = new(Keywords.LOWER, source);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
