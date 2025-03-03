namespace InlineSqlSharp;

public sealed class RtrimFunction(
	CharacterExpr source,
	CharacterExpr? trimChars = null) : CharacterExpr
{
	private readonly VariadicFunctionCore _core =
		new(Keywords.RTRIM, source, trimChars);

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
