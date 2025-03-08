namespace InlineSqlSharp;

public sealed class CharacterGreatestFunction(CharacterExpr[] expressions)
: CharacterExpr
{
	private readonly VariadicFunctionCore _core = new(Keywords.GREATEST, expressions);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
