namespace InlineSqlSharp;

public sealed class SubstrbFunction : CharacterExpr
{
	private readonly SubstrFunctionCore _core;

	public static SubstrbFunction Of(
		CharacterExpr source,
		NumericExpr position) => new(source, position, null);

	public static SubstrbFunction Of(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr length) => new(source, position, length);

	public override void FormatSql(SqlBuildingBuffer buffer) => _core.FormatSql(buffer);

	private SubstrbFunction(
		CharacterExpr source,
		NumericExpr position,
		NumericExpr? length)
	{
		_core = new(Keywords.SUBSTRB, source, position, length);
	}
}
