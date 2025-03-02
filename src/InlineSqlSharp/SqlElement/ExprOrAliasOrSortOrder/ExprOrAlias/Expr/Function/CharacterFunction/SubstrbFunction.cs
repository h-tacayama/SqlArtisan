namespace InlineSqlSharp;

public sealed class SubstrbFunction : CharacterExpr
{
	private readonly SubstrFunctionCore _core;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);

	internal static SubstrbFunction Of(
		CharacterExpr source,
		NumericExpr position) => new(source, position, null);

	internal static SubstrbFunction Of(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	private SubstrbFunction(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr? length)
	{
		_core = new(Keywords.SUBSTRB, source, position, length);
	}
}
