namespace InlineSqlSharp;

public sealed class UpperFunction(CharacterExpr source) : CharacterExpr
{
	private readonly UnaryFunctionCore _core = new(Keywords.UPPER, source);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
