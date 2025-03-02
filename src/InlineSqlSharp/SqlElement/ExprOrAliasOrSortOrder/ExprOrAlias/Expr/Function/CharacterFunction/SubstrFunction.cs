namespace InlineSqlSharp;

public sealed class SubstrFunction : CharacterExpr
{
	private readonly SubstrFunctionCore _core;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);

	internal static SubstrFunction Of(
		CharacterExpr source,
		NumericExpr position) => new(source, position, null);

	internal static SubstrFunction Of(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	private SubstrFunction(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr? length)
	{
		_core = new(Keywords.SUBSTR, source, position, length);
	}
}
